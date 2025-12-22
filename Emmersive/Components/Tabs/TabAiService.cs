using System;
using System.Linq;
using Cwl.Helper.FileUtil;
using Cwl.LangMod;
using Emmersive.API;
using Emmersive.API.Services;
using Emmersive.ChatProviders;
using Emmersive.Helper;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Components;

internal class TabAiService : TabEmmersiveBase
{
    private UIButton? _schedulerMode;

    public override void OnLayout()
    {
        BuildSchedulerButton();
        BuildDebugButtons();

        if (EmActivity.Session.Count > 0) {
            this.MakeCard().ShowActivityInfo("");
        }

        BuildServiceButtons();

        var layouts = ApiPoolSelector.Instance.Providers
            .OfType<ILayoutProvider>()
            .ToArray();

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
                if (index < initialIndex || index >= layouts.Length + initialIndex) {
                    return;
                }

                card.transform.SetSiblingIndex(index);
                ApiPoolSelector.Instance.ReorderService(chatProvider, a);
            }
        }
    }

    public override void OnLayoutConfirm()
    {
        var layouts = ApiPoolSelector.Instance
            .Providers
            .OfType<ILayoutProvider>();
        foreach (var provider in layouts) {
            provider.OnLayoutConfirm();
        }

        base.OnLayoutConfirm();
    }

    private void BuildServiceButtons()
    {
        var btnGroup = Horizontal()
            .WithSpace(10);
        btnGroup.Layout.childForceExpandWidth = true;

        AddServiceButton("em_ui_add_service_google", apiKey => new GoogleProvider(apiKey));
        AddServiceButton("em_ui_add_service_openai", apiKey => new OpenAIProvider(apiKey));

        // CN treat: piexian free API
        if (Lang.langCode == "CN") {
            btnGroup.Button("em_ui_add_service_piexian".lang(),
                () => Dialog.YesNo("em_ui_px_desc", () => {
                    Application.OpenURL("https://proxy.pieixan.icu/login");
                    AddService(new PiexianProvider());
                }));
        }

        return;

        void AddServiceButton(string btnName, Func<string, IChatProvider> serviceFactory)
        {
            btnGroup.Button(btnName.lang(), () => {
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
            });
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

        btnGroup.Button("em_ui_config_open".lang(), () => OpenFileOrPath.Run(EmMod.Instance.Config.ConfigFilePath));

        var link = "https://elin-modding.net/articles/100_Mod%20Documentation/Emmersive/API_Setup" +
                   Lang.langCode switch {
                       "CN" or "ZHTW" => ".CN",
                       "JP" => ".JP",
                       _ => "",
                   };

        btnGroup.Button("em_ui_api_guide".lang(), () => Application.OpenURL(link))
            .mainText.supportRichText = true;

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

    private static void AddService(IChatProvider provider)
    {
        var apiPool = ApiPoolSelector.Instance;
        apiPool.AddService(provider);
        apiPool.ReorderService(provider, 1 - apiPool.Providers.Count);
        EmKernel.RebuildKernel();
        LayerEmmersivePanel.Instance?.Reopen();
    }
}