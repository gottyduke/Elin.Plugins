using System;
using System.Linq;
using Cwl.Helper.FileUtil;
using Emmersive.API;
using Emmersive.API.Services;
using Emmersive.ChatProviders;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Components;

internal class TabAiService : TabEmmersiveBase
{
    private static Vector2 _browsedPosition = new(0f, 1f);

    private bool _repaint;

    private void Update()
    {
        if (_repaint) {
            _browsedPosition = GetComponentInParent<UIScrollView>().normalizedPosition;
        }
    }

    public override void OnLayout()
    {
        BuildDebugButtons();
        BuildServiceButtons();

        var layouts = ApiPoolSelector.Instance.Providers
            .OfType<ILayoutProvider>()
            .ToArray();

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
                if (index < 2 || index >= layouts.Length + 2) {
                    return;
                }

                card.transform.SetSiblingIndex(index);
                ApiPoolSelector.Instance.ReorderService(chatProvider, a);
            }
        }

        GetComponentInParent<UIScrollView>().normalizedPosition = _browsedPosition;
        _repaint = true;
    }

    public override void OnLayoutConfirm()
    {
        var layouts = ApiPoolSelector.Instance
            .Providers
            .OfType<ILayoutProvider>();
        foreach (var provider in layouts) {
            provider.OnLayoutConfirm();
        }
    }

    private void BuildDebugButtons()
    {
        var btnGroup = Horizontal()
            .WithSpace(10);
        btnGroup.Layout.childForceExpandWidth = true;

        btnGroup.Button("em_ui_test_generation".lang(), () => {
            LayerEmmersivePanel.Instance!.OnLayoutConfirm();
            EmScheduler.RequestScenePlayImmediate();
            ELayer.ui.RemoveLayer<LayerEmmersivePanel>();
        });

        btnGroup.Button("em_ui_config_open".lang(), () => {
            // TODO: use CWL version after updating
            OpenFileOrPath.Run(EmMod.Instance.Config.ConfigFilePath);
        });
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
                () => Dialog.YesNo("em_ui_px_desc", () => AddService(new PiexianProvider())));
        }

        return;

        void AddServiceButton(string btnName, Func<string, IChatProvider> serviceFactory)
        {
            btnGroup.Button(btnName.lang(), () => {
                var d = Dialog.InputName(
                    "em_ui_paste_api_key".lang(),
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

    private void AddService(IChatProvider provider)
    {
        var apiPool = ApiPoolSelector.Instance;
        apiPool.AddService(provider);
        apiPool.ReorderService(provider, 1 - apiPool.Providers.Count);
        EmKernel.RebuildKernel();
        LayerEmmersivePanel.Instance?.Reopen(name);
    }
}