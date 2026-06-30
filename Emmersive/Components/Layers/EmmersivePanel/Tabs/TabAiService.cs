using System;
using System.Collections.Generic;
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
    private UIButton? _schedulerMode;
    private int _selectedServiceIndex;

    public override void OnLayout()
    {
        ChatProviderBase.OnProviderRemoved = provider => {
            ApiPoolSelector.Instance.RemoveService(provider);
            EmKernel.RebuildKernel();
        };
        ChatProviderBase.OnProviderConfigChanged = () => EmKernel.RebuildKernel();

        BuildButtons();

        if (EmActivity.Session.Count > 0) {
            this.MakeCard().ShowActivityInfo("");
        }

        var layouts = ApiPoolSelector.Instance.Providers
            .OfType<ILayoutProvider>()
            .Concat(EmPluginRegistry.Instance.ExternalLayoutProviders)
            .ToList();

        var initialIndex = transform.childCount;
        foreach (var provider in layouts) {
            var card = Horizontal();

            provider.OnLayout(card);

            if (provider is IChatProvider chatProvider) {
                var move = card.Vertical();
                move.LayoutElement().flexibleWidth = 0f;
                move.Fitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
                card.Layout.childForceExpandHeight = true;

                move.Button("↑", () => Reorder(-1));
                move.Button("↓", () => Reorder(1));
            }

            continue;

            void Reorder(int a)
            {
                var index = card.transform.GetSiblingIndex() + a;
                if (index < initialIndex || index >= layouts.Count + initialIndex) {
                    return;
                }

                card.transform.SetSiblingIndex(index);
                ApiPoolSelector.Instance.ReorderService(chatProvider, a);
            }
        }
    }

    public override void OnLayoutConfirm()
    {
        var layouts = ApiPoolSelector.Instance.Providers
            .OfType<ILayoutProvider>()
            .Concat(EmPluginRegistry.Instance.ExternalLayoutProviders);
        foreach (var provider in layouts) {
            provider.OnLayoutConfirm();
        }

        base.OnLayoutConfirm();
    }

    private void BuildButtons()
    {
        BuildSchedulerButton();
        BuildDebugButtons();

        BuildServiceButtons();
    }

    private void BuildServiceButtons()
    {
        var btnGroup = Horizontal()
            .WithSpace(10);
        btnGroup.Layout.childForceExpandWidth = true;

        var serviceOptions = new List<string> {
            "em_ui_add_service_openai".lang(),
            "em_ui_add_service_deepseek".lang(),
            "em_ui_add_service_google".lang(),
            "em_ui_add_service_player2".lang(),
            "em_ui_add_service_ollama".lang(),
        };
        if (Lang.langCode is "CN" or "ZHTW") {
            serviceOptions.Add("em_ui_add_service_piexian".lang());
        }

        var dropdown = btnGroup.Dropdown(
            serviceOptions,
            idx => _selectedServiceIndex = idx,
            _selectedServiceIndex);
        dropdown.GetOrCreate<LayoutElement>().minWidth = 200f;

        btnGroup.Button("em_ui_add".lang(), OnAddClicked)
            .GetOrCreate<Image>().color = Color.green;

        return;

        void OnAddClicked()
        {
            switch (_selectedServiceIndex) {
                case 0:
                    AddServiceButton(apiKey => new OpenAIProvider(apiKey));
                    break;
                case 1:
                    AddServiceButton(apiKey => new DeepSeekProvider(apiKey));
                    break;
                case 2:
                    AddServiceButton(apiKey => new GoogleProvider(apiKey));
                    break;
                case 3:
                    Dialog.YesNo("em_ui_p2_desc", () => {
                        AddService(new Player2Provider());
                    });
                    break;
                case 4:
                    AddService(new OllamaProvider());
                    break;
                case 5:
                    Dialog.YesNo("em_ui_px_desc", () => {
                        Application.OpenURL("https://api.pie-xian.com/");
                        AddService(new PiexianProvider());
                    });
                    break;
            }
        }

        void AddServiceButton(Func<string, IChatProvider> serviceFactory)
        {
            var d = Dialog.InputName(
                "em_ui_paste_api_key",
                "em_ui_api_key".lang(),
                (cancel, apiKey) => {
                    if (!cancel) {
                        AddService(serviceFactory(apiKey));
                    }
                });
            d.input.field.characterLimit = 200;
            d.input.field.contentType = InputField.ContentType.Password;
            d.input.field.text = "";
        }
    }

    private void BuildSchedulerButton()
    {
        var btnGroup = Horizontal();
        btnGroup.Layout.childForceExpandWidth = true;

        _schedulerMode = btnGroup.Toggle(
            GetCurrentSchedulerState(),
            EmScheduler.Mode is EmScheduler.SchedulerMode.Buffer or EmScheduler.SchedulerMode.Immediate,
            value => {
                EmScheduler.SwitchMode(value ? EmScheduler.SchedulerMode.Buffer : EmScheduler.SchedulerMode.Stop);
                _schedulerMode?.mainText.text = GetCurrentSchedulerState();
            });
        _schedulerMode.GetOrCreate<LayoutElement>().minWidth = 80f;

        btnGroup.Button("em_ui_config_open".lang(), () => Util.Run(EmMod.Instance.Config.ConfigFilePath));

        btnGroup.Button("em_ui_api_guide".lang(), OpenApiGuide)
            .mainText.supportRichText = true;

        var chatKey = EClass.core.config.input.keys.chat;
        btnGroup.Button("em_ui_chat_keymap".Loc(chatKey.key.ToString()), () => Dialog.Keymap(chatKey).SetOnKill(() => {
            EmConfig.Policy.PlayerTalkKey.Value = chatKey.key;
            LayerEmmersivePanel.Instance?.Reopen();
        }));

        return;

        string GetCurrentSchedulerState()
        {
            var isOn = EmScheduler.Mode is EmScheduler.SchedulerMode.Buffer or EmScheduler.SchedulerMode.Immediate;
            return "em_ui_scheduler_toggle".Loc((isOn ? "on" : "off").lang());
        }
    }

    private void BuildDebugButtons()
    {
        var btnGroup = Horizontal()
            .WithSpace(10);
        btnGroup.Layout.childForceExpandWidth = true;

        btnGroup.Button("em_ui_test_generation".lang(), TestRun);

        btnGroup.Button("em_ui_scheduler_dry".lang(), () => {
            EmScheduler.SwitchMode(EmScheduler.SchedulerMode.DryRun);
            TestRun();
        });

        return;

        void TestRun()
        {
            LayerEmmersivePanel.Instance!.OnLayoutConfirm();
            ELayer.ui.RemoveLayer<LayerEmmersivePanel>();
            EmScheduler.RequestScenePlayImmediate();
        }
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

    private static void AddService(IChatProvider provider)
    {
        var apiPool = ApiPoolSelector.Instance;
        apiPool.AddService(provider);
        apiPool.ReorderService(provider, 1 - apiPool.Providers.Count);
        EmKernel.RebuildKernel();
        LayerEmmersivePanel.Instance?.Reopen();
    }
}