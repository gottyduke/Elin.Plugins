using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using EModding.Helper.Runtime;
using UnityEngine;
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
                note.AddHeader(section.Key);
                foreach (var entry in section) {
                    note.Add(entry);
                }
            }
        }

        public void Add(ConfigEntryBase entry)
        {
            var type = entry.SettingType;
            var label = Label(entry);
            var acceptable = entry.Description.AcceptableValues;
            var range = acceptable?.GetType() is { IsGenericType: true } t ? t.GetGenericTypeDefinition() : null;
            var slider = !type.IsEnum && Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal &&
                         range == typeof(AcceptableValueRange<>);
            float min = 0;
            float max = 0;
            if (slider) {
                min = Convert.ToSingle(Prop("MinValue"));
                max = Convert.ToSingle(Prop("MaxValue"));
                slider = Math.Max(Math.Abs(min), Math.Abs(max)) <= 1 << 24;
            }

            var item = Util.Instantiate<UIItem>(CorePath.UI.Layer + "LayerMod/ItemModConfig", note.layout);
            var content = item.transform.Find("Content");
            var count = note.layout.transform.childCount;
            Action refresh;
            if (type == typeof(bool)) {
                var toggle = note.AddToggle(label, (bool)entry.BoxedValue, on => Set(entry, on));
                refresh = () => toggle.SetCheck((bool)entry.BoxedValue);
            } else if (slider) {
                var s = note.AddSlider(entry, min, max);
                refresh = () => s.value = Convert.ToSingle(entry.BoxedValue);
            } else if (type.IsEnum || range == typeof(AcceptableValueList<>)) {
                List<object> values = [
                    ..(range == typeof(AcceptableValueList<>) ? (Array)Prop("AcceptableValues") : Enum.GetValues(type))
                    .OfType<object>()
                    .Where(v => acceptable?.IsValid(v) ?? true),
                ];
                var dropdown = DropdownOf(note, entry, values, label);
                refresh = () => {
                    var index = values.IndexOf(entry.BoxedValue);
                    if (index >= 0) {
                        dropdown.value = index;
                    }
                };
            } else {
                refresh = InputOf(note, entry, label);
            }

            UIText? desc = null;
            if (!entry.Description.Description.IsEmpty()) {
                desc = note.AddText(entry.Description.Description, FontColor.Passive).text1;
                desc.SetSize(-3);
            }

            while (note.layout.transform.childCount > count) {
                note.layout.transform.GetChild(count).SetParent(content, false);
            }

            note.RebuildLayout();
            desc?.SetText(entry.Description.Description);
            refresh();
            item.button1.SetOnClick(() => {
                Set(entry, entry.DefaultValue);
                refresh();
                note.Build();
            });

            return;

            object Prop(string name)
            {
                return acceptable!.GetType().GetProperty(name)!.GetValue(acceptable);
            }
        }

        public Slider AddSlider(ConfigEntryBase entry, float min, float max, string? label = null)
        {
            label = label?.lang() ?? Label(entry);
            var init = entry.BoxedValue;
            var f = Mathf.Clamp(Convert.ToSingle(init), min, max);
            return note.AddSlider(f, v => {
                Set(entry, Mathf.Approximately(v, f) ? init : Convert.ChangeType(v, entry.SettingType));
                return $"{label} ({v:0.##})";
            }, min, max, Type.GetTypeCode(entry.SettingType) is >= TypeCode.SByte and <= TypeCode.UInt64);
        }

        public UIDropdown AddDropdown<T>(ConfigEntry<T> entry, IList<T> values, string? label = null)
        {
            return DropdownOf(note, entry, [..values.Cast<object>()], label ?? Label(entry));
        }
    }

    private static UIDropdown DropdownOf(UINote note, ConfigEntryBase entry, List<object> values, string label)
    {
        if (!values.Contains(entry.BoxedValue)) {
            values.Insert(0, entry.BoxedValue);
        }

        var dropdown = note.AddDropdown(label);
        dropdown.SetList(values.IndexOf(entry.BoxedValue), values, (v, _) => v.ToString(), (_, v) => Set(entry, v), false);
        return dropdown;
    }

    private static Action InputOf(UINote note, ConfigEntryBase entry, string label)
    {
        UIButton button = null!;
        button = note.AddButton("", () => {
            Dialog.InputName(label, Text(), (cancel, text) => {
                if (cancel) {
                    return;
                }

                try {
                    Set(entry,
                        entry.SettingType == typeof(string) ? text : TomlTypeConverter.ConvertToValue(text, entry.SettingType));
                    Refresh();
                    note.Build();
                } catch (Exception ex) {
                    SE.Beep();
                    EClass.ui.Say(ex.Message);
                }
            }, Dialog.InputType.Chat);
        });
        button.mainText.GetComponent<ContentSizeFitter>().enabled = false;
        button.mainText.rectTransform.sizeDelta = new(-20, 0);
        button.mainText.horizontalOverflow = HorizontalWrapMode.Wrap;

        return Refresh;

        string Text()
        {
            return entry.SettingType == typeof(string) ? (string)entry.BoxedValue : entry.GetSerializedValue();
        }

        void Refresh()
        {
            button.mainText.text = $"{label}: {Text()}";
            var size = button.Rect().sizeDelta;
            button.Rect().sizeDelta = size with { y = Mathf.Max(50, button.mainText.preferredHeight + 20) };
        }
    }

    private static string Label(ConfigEntryBase entry)
    {
        return entry.Definition.Key.lang();
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