using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using EModding.Helper.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EModding;

public static class ModConfig
{
    internal static void RegisterAll()
    {
        foreach (var plugin in TypeQualifier.Plugins) {
            if (plugin.Info.Location.IsEmpty() || (plugin.Config.Count == 0 && plugin is not IModConfig)) {
                continue;
            }

            var package = ModUtil.FindFileProviderPackage(new(plugin.Info.Location));
            if (package is null) {
                continue;
            }

            package.onBuildConfig += note => Build(plugin, note);
            if (plugin.Config.Count > 0) {
                package.onResetConfig += () => Reset(plugin.Config);
                package.configPath = plugin.Config.ConfigFilePath;
            }
        }
    }

    private static void Build(BaseUnityPlugin plugin, UINote note)
    {
        if (plugin is IModConfig custom) {
            custom.OnBuildConfig(note);
        } else {
            note.AddAll(plugin.Config);
        }
    }

    extension(UINote note)
    {
        public void AddAll(ConfigFile config)
        {
            var first = true;
            foreach (var section in config.Values.GroupBy(e => e.Definition.Section)) {
                if (!first) {
                    note.Space();
                }

                first = false;
                note.AddHeader(Text(section.Key));
                foreach (var entry in section) {
                    note.Add(entry);
                }
            }
        }

        public void Add(ConfigEntryBase entry)
        {
            var type = entry.SettingType;
            var acceptable = entry.Description.AcceptableValues;
            var range = acceptable?.GetType() is { IsGenericType: true } t ? t.GetGenericTypeDefinition() : null;
            if (!type.IsEnum && Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal &&
                range == typeof(AcceptableValueRange<>)) {
                var min = Convert.ToSingle(Prop("MinValue"));
                var max = Convert.ToSingle(Prop("MaxValue"));
                if (Math.Max(Math.Abs(min), Math.Abs(max)) <= 1 << 24) {
                    note.AddSlider(entry, min, max);
                    return;
                }
            } else if (type == typeof(KeyCode)) {
                Row(note, entry, null, item => {
                    var button = item.button3;
                    button.SetActive(true);
                    button.SetOnClick(() => {
                        var keymap = new EInput.KeyMap { key = (KeyCode)entry.BoxedValue };
                        var dialog = Dialog.Keymap(keymap);
                        dialog.textDetail.SetText("dialog_keymap".lang(item.text1.text));
                        dialog.onKill.AddListener(() => Set(entry, keymap.key));
                    });
                    return () => button.mainText.text = Text(entry.BoxedValue.ToString());
                });
                return;
            } else if (type.IsEnum || range == typeof(AcceptableValueList<>)) {
                DropdownOf(note, entry, [
                    ..(range == typeof(AcceptableValueList<>) ? (Array)Prop("AcceptableValues") : Enum.GetValues(type))
                    .OfType<object>()
                    .Where(v => acceptable?.IsValid(v) ?? true),
                ], null);
                return;
            }

            Row(note, entry, null, item => {
                if (type != typeof(bool)) {
                    return InputOf(item, entry);
                }

                var toggle = item.button2;
                toggle.SetActive(true);
                toggle.SetToggle((bool)entry.BoxedValue, on => Set(entry, on));
                return () => toggle.SetCheck((bool)entry.BoxedValue);
            });
            return;

            object Prop(string name)
            {
                return acceptable!.GetType().GetProperty(name)!.GetValue(acceptable);
            }
        }

        public Slider AddSlider(ConfigEntryBase entry, float min, float max, string? label = null)
        {
            var init = entry.BoxedValue;
            var f = Mathf.Clamp(Convert.ToSingle(init), min, max);
            return Row(note, entry, label, item => {
                var s = item.GetComponentInChildren<Slider>(true);
                s.SetActive(true);
                s.minValue = min;
                s.maxValue = max;
                s.wholeNumbers = Type.GetTypeCode(entry.SettingType) is >= TypeCode.SByte and <= TypeCode.UInt64;
                s.onValueChanged.AddListener(v =>
                    Set(entry,
                        Mathf.Approximately(v, f)
                            ? init
                            : Convert.ChangeType(s.wholeNumbers ? v : (decimal)v, entry.SettingType)));
                var input = item.text3.GetComponentInParent<InputField>();
                input.contentType = s.wholeNumbers ? InputField.ContentType.IntegerNumber : InputField.ContentType.Standard;
                var shown = "";
                input.onEndEdit.AddListener(text => {
                    if (!input.wasCanceled && text != shown) {
                        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) {
                            var value = Convert.ChangeType(Math.Max((decimal)min, Math.Min((decimal)max, d)), entry.SettingType);
                            BaseCore.Instance.actionsNextFrame.Add(() => Set(entry, value));
                        } else {
                            SE.Beep();
                        }
                    }

                    input.text = shown = Convert.ToString(entry.BoxedValue, CultureInfo.InvariantCulture);
                    input.ForceLabelUpdate();
                    if (!EventSystem.current.alreadySelecting) {
                        EventSystem.current.SetSelectedGameObject(null);
                    }
                });
                return () => {
                    s.SetValueWithoutNotify(Convert.ToSingle(entry.BoxedValue));
                    if (!input.isFocused) {
                        input.text = shown = Convert.ToString(entry.BoxedValue, CultureInfo.InvariantCulture);
                    }
                };
            }).GetComponentInChildren<Slider>();
        }

        public UIDropdown AddDropdown<T>(ConfigEntry<T> entry, IList<T> values, string? label = null)
        {
            return DropdownOf(note, entry, [..values.Cast<object>()], label);
        }
    }

    private static UIItem Row(UINote note, ConfigEntryBase entry, string? label, Func<UIItem, Action> bind)
    {
        var item = Util.Instantiate<UIItem>(CorePath.UI.Layer + "LayerMod/ItemModConfig", note.layout);
        item.refObj = entry;
        item.text1.SetText(label?.lang() ?? Text(entry.Definition.Key));
        var refresh = bind(item);
        var desc = entry.Description.Description;
        item.text2.SetActive(!desc.IsEmpty());
        note.RebuildLayout();
        if (!desc.IsEmpty()) {
            item.text2.SetText(desc.lang());
        }

        refresh();
        SetMark();
        item.button1.SetOnClick(() => Set(entry, entry.DefaultValue));
        EventHandler<SettingChangedEventArgs> onChanged = null!;
        entry.ConfigFile.SettingChanged += onChanged = (_, e) => {
            if (!item) {
                entry.ConfigFile.SettingChanged -= onChanged;
            } else if (e.ChangedSetting == entry) {
                refresh();
                SetMark();
            }
        };
        var i = item.transform.GetSiblingIndex();
        var prev = i > 0 ? note.layout.transform.GetChild(i - 1).GetComponent<UIItem>() : null;
        item.image1.SetActive(prev?.refObj is ConfigEntryBase && !prev.image1.gameObject.activeSelf);
        return item;

        void SetMark()
        {
            item.button1.image.enabled = item.button1.icon.enabled =
                entry.GetSerializedValue() != TomlTypeConverter.ConvertToString(entry.DefaultValue, entry.SettingType);
        }
    }

    private static UIDropdown DropdownOf(UINote note, ConfigEntryBase entry, List<object> values, string? label)
    {
        if (!values.Contains(entry.BoxedValue)) {
            values.Insert(0, entry.BoxedValue);
        }

        return Row(note, entry, label, item => {
            var dropdown = item.dd;
            dropdown.SetActive(true);
            var caption = dropdown.captionText;
            dropdown.captionText = null;
            dropdown.SetList(values.IndexOf(entry.BoxedValue), values, (v, _) => Text(v.ToString()), (_, v) => {
                Set(entry, v);
                Show();
            }, false);
            Show();
            return () => {
                var index = values.IndexOf(entry.BoxedValue);
                if (index >= 0) {
                    dropdown.value = index;
                }
            };

            void Show()
            {
                caption.text = caption.Fit(dropdown.options[dropdown.value].text);
            }
        }).dd;
    }

    private static Action InputOf(UIItem item, ConfigEntryBase entry)
    {
        var button = item.button3;
        button.SetActive(true);
        button.SetOnClick(() => {
            Dialog.InputName(item.text1.text, GetInputText(), (cancel, text) => {
                if (cancel) {
                    return;
                }

                try {
                    Set(entry,
                        entry.SettingType == typeof(string) ? text : TomlTypeConverter.ConvertToValue(text, entry.SettingType));
                } catch (Exception ex) {
                    SE.Beep();
                    EClass.ui.Say(ex.Message);
                }
            }, Dialog.InputType.Chat);
        });

        return () => button.mainText.text = button.mainText.Fit(GetInputText());

        string GetInputText()
        {
            return entry.SettingType == typeof(string) ? (string)entry.BoxedValue : entry.GetSerializedValue();
        }
    }

    private static string Text(string id)
    {
        if (Lang.Has(id) || id.Contains(' ')) {
            return id.lang();
        }

        var s = Regex.Replace(id.Replace('_', ' '), "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
        return s.IsEmpty() ? s : char.ToUpper(s[0]) + s[1..];
    }

    private static void Set(ConfigEntryBase entry, object value)
    {
        if (Equals(entry.BoxedValue, value)) {
            return;
        }

        entry.BoxedValue = value;
        if (!entry.ConfigFile.SaveOnConfigSet) {
            entry.ConfigFile.Save();
        }
    }

    private static void Reset(ConfigFile config)
    {
        var save = config.SaveOnConfigSet;
        config.SaveOnConfigSet = false;
        foreach (var entry in config.Values) {
            entry.BoxedValue = entry.DefaultValue;
        }

        config.SaveOnConfigSet = save;
        config.Save();
    }
}