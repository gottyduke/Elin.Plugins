using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emmersive.API;
using Emmersive.API.Services;
using Emmersive.API.ThirdParty;
using Emmersive.ChatProviders;
using Emmersive.Helper;
using Emmersive.LangMod;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Components;

internal class TabAiService : TabEmmersiveBase
{
    private static int _selectedServiceIndex;

    private UIButton? _schedulerToggle;

    public override void OnLayout()
    {
        BuildAddServiceRow();
        BuildControlRow();

        var providers = ApiPoolSelector.Instance.Providers;
        if (providers.Count == 0 && ApiPoolSelector.Instance.LoadFailedBackupDir is { } backup) {
            BuildLoadErrorCard(backup);
        } else if (providers.Count == 0) {
            BuildEmptyGuide();
        } else if (providers.Count > 1) {
            TextFlavor("em_ui_pool_order_hint");
        }

        foreach (var provider in providers.OfType<ILayoutProvider>().ToArray()) {
            BuildServiceCard(provider);
        }

        var externals = EmPluginRegistry.Instance.ExternalLayoutProviders;
        if (externals.Count == 0) {
            return;
        }

        HeaderCard("em_ui_external_section");
        foreach (var external in externals.ToArray()) {
            try {
                external.OnLayout(Horizontal());
            } catch (Exception ex) {
                EmMod.Warn<TabAiService>($"external layout {external.GetType().Name} failed: {ex.Message}");
            }
        }
    }

    public override void OnLayoutConfirm()
    {
        foreach (var provider in ApiPoolSelector.Instance.Providers.OfType<ILayoutProvider>().ToArray()) {
            provider.OnLayoutConfirm();
        }

        foreach (var external in EmPluginRegistry.Instance.ExternalLayoutProviders.ToArray()) {
            try {
                external.OnLayoutConfirm();
            } catch (Exception ex) {
                EmMod.Warn<TabAiService>($"external layout {external.GetType().Name} failed: {ex.Message}");
            }
        }

        EmKernel.RebuildKernel();
        SavePool();

        base.OnLayoutConfirm();
    }

    internal static void SavePool()
    {
        ApiPoolSelector.Instance.SaveServices();
    }

    internal static void RequestTestScene(bool dryRun)
    {
        if (!EmScheduler.CheckSceneRequest(dryRun)) {
            return;
        }

        ELayer.ui.RemoveLayer<LayerEmmersivePanel>();

        if (dryRun) {
            EmScheduler.SwitchMode(EmScheduler.SchedulerMode.DryRun);
        }

        EmScheduler.RequestScenePlayImmediate();
    }

    private static List<(string LangKey, Action Add)> GetServiceOptions()
    {
        List<(string LangKey, Action Add)> options = [
            ("em_ui_add_service_openai", () => AddWithApiKey(new OpenAIProvider(""))),
            ("em_ui_add_service_deepseek", () => AddWithApiKey(new DeepSeekProvider(""))),
            ("em_ui_add_service_google", () => AddWithApiKey(new GoogleProvider(""))),
            ("em_ui_add_service_player2", () => Dialog.YesNo("em_ui_p2_desc", () => AddService(new Player2Provider()))),
            ("em_ui_add_service_ollama", AddOllama),
        ];

        if (Lang.langCode is "CN" or "ZHTW") {
            options.Add(("em_ui_add_service_piexian", () => Dialog.YesNo("em_ui_px_desc", () => {
                Application.OpenURL("https://api.pie-xian.com/");
                AddWithApiKey(new PiexianProvider());
            })));
        }

        return options;

        static void AddWithApiKey(ChatProviderBase provider)
        {
            ChatProviderBase.AskApiKey(provider.Alias, key => {
                provider.ApplyApiKey(key);
                AddService(provider);
            });
        }

        static void AddOllama()
        {
            var d = Dialog.InputName(
                "em_ui_ollama_model",
                "",
                (cancel, model) => {
                    model = model.Trim();
                    if (!cancel && !model.IsEmptyOrNull) {
                        AddService(new OllamaProvider { CurrentModel = model });
                    }
                });
            d.input.field.characterLimit = 100;
        }
    }

    private static void AddService(IChatProvider provider)
    {
        var apiPool = ApiPoolSelector.Instance;
        apiPool.AddService(provider);
        EmKernel.RebuildKernel();
        LayerEmmersivePanel.Instance?.Reopen();
    }

    private static void OpenApiGuide()
    {
        var lang = Lang.langCode switch {
            "CN" or "ZHTW" => "/zh",
            "JP" => "/ja",
            _ => "",
        };
        var link = $"https://elin-modding.net{lang}/articles/100_Mod%20Documentation/Emmersive/API_Setup";

        Application.OpenURL(link);
    }

    private void BuildAddServiceRow()
    {
        var options = GetServiceOptions();
        _selectedServiceIndex = Mathf.Clamp(_selectedServiceIndex, 0, options.Count - 1);

        var btnGroup = Horizontal()
            .WithSpace(10);
        btnGroup.Layout.childForceExpandWidth = true;

        var dropdown = btnGroup.Dropdown(
            [..options.Select(o => o.LangKey.lang())],
            idx => _selectedServiceIndex = idx,
            _selectedServiceIndex);
        dropdown.GetOrCreate<LayoutElement>().minWidth = 200f;

        btnGroup.Button("em_ui_add".lang(), () => options[_selectedServiceIndex].Add())
            .GetOrCreate<Image>().color = Color.green;
    }

    private void BuildControlRow()
    {
        var btnGroup = Horizontal()
            .WithSpace(10);
        btnGroup.Layout.childForceExpandWidth = true;

        _schedulerToggle = btnGroup.Toggle(
            GetSchedulerState(),
            EmScheduler.IsEnabled,
            value => {
                if (value != EmScheduler.IsEnabled) {
                    EmScheduler.Toggle();
                }

                _schedulerToggle?.mainText.text = GetSchedulerState();
            });
        _schedulerToggle.GetOrCreate<LayoutElement>().minWidth = 80f;
        _schedulerToggle.SetInteractableWithAlpha(ApiPoolSelector.Instance.Providers.Count > 0);

        btnGroup.Button("em_ui_test_generation".lang(), () => RequestTestScene(false));

        var chatKey = EClass.core.config.input.keys.chat;
        btnGroup.Button("em_ui_chat_keymap".Loc(chatKey.key.ToString()), () => Dialog.Keymap(chatKey).SetOnKill(() => {
            LayerEmmersivePanel.Instance?.Reopen();
        }));

        btnGroup.Button("em_ui_config_open".lang(), () => LayerModConfig.Open(ModUtil.GetModPackage(ModInfo.Guid)));

        btnGroup.Button("em_ui_api_guide".lang(), OpenApiGuide)
            .mainText.supportRichText = true;

        return;

        static string GetSchedulerState() => "em_ui_scheduler_toggle".Loc((EmScheduler.IsEnabled ? "on" : "off").lang());
    }

    private void BuildEmptyGuide()
    {
        var guide = this.MakeCard();
        guide.TextLong("em_ui_empty_guide");
        guide.Spacer(5);
        guide.Button("em_ui_api_guide".lang(), OpenApiGuide)
            .mainText.supportRichText = true;
    }

    private void BuildLoadErrorCard(string backup)
    {
        var card = this.MakeCard();
        card.Layout.GetOrCreate<Image>().color = Color.red;
        card.TextLong("em_ui_services_load_failed".Loc(backup));
        card.Spacer(5);
        card.Button("em_ui_open_folder".lang(), () => {
            Directory.CreateDirectory(backup);
            Util.Run(backup);
        });
    }

    private void BuildServiceCard(ILayoutProvider provider)
    {
        var card = Horizontal();

        provider.OnLayout(card);

        if (provider is not IChatProvider chatProvider) {
            return;
        }

        var move = card.Vertical();
        move.LayoutElement().flexibleWidth = 0f;
        move.Fitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
        card.Layout.childForceExpandHeight = true;

        var providers = ApiPoolSelector.Instance.Providers;
        var current = providers.ToList().IndexOf(chatProvider);

        move.Button("↑", () => Reorder(-1))
            .SetInteractableWithAlpha(current > 0);
        move.Button("↓", () => Reorder(1))
            .SetInteractableWithAlpha(current >= 0 && current < providers.Count - 1);

        return;

        void Reorder(int offset)
        {
            var pool = ApiPoolSelector.Instance;
            var index = pool.Providers.ToList().IndexOf(chatProvider);
            var target = index + offset;
            if (index < 0 || target < 0 || target >= pool.Providers.Count) {
                return;
            }

            pool.ReorderService(chatProvider, offset);
            LayerEmmersivePanel.Instance?.Reopen();
        }
    }
}