using System;
using System.Linq;
using Emmersive.API;
using Emmersive.API.Services;
using Emmersive.ChatProviders;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Components;

internal class TabAiService : TabEmmersiveBase
{
    public override void OnLayout()
    {
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

        base.OnLayout();
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

    private void AddService(IChatProvider provider)
    {
        var apiPool = ApiPoolSelector.Instance;
        apiPool.AddService(provider);
        apiPool.ReorderService(provider, 1 - apiPool.Providers.Count);
        EmKernel.RebuildKernel();
        LayerEmmersivePanel.Instance?.Reopen();
    }
}