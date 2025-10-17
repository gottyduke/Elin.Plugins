using System;
using System.Linq;
using Cwl.Helper.FileUtil;
using Emmersive.API;
using Emmersive.API.Services;
using Emmersive.ChatProviders;
using Emmersive.Contexts;
using Emmersive.Helper;
using UnityEngine;
using UnityEngine.UI;
using YKF;
using OpenFileOrPath = Emmersive.Helper.OpenFileOrPath;

namespace Emmersive.Components;

internal class TabAiService : TabEmmersiveBase
{
    private static Vector2 _browsedPosition;

    private void Update()
    {
        _browsedPosition = GetComponentInParent<UIScrollView>().normalizedPosition;
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
                AddReorderButton();
            }

            continue;

            void AddReorderButton()
            {
                var move = card.Vertical();
                move.LayoutElement().flexibleWidth = 0f;
                move.Fitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
                card.Layout.childForceExpandHeight = true;

                move.Button("↑", () => Reorder(-1));
                move.Button("↓", () => Reorder(1));
            }

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
    }

    public override void OnLayoutConfirm()
    {
        var layouts = ApiPoolSelector.Instance.Providers
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

        btnGroup.Button("em_ui_reload_prompts".lang(), () => {
            ResourceFetch.ClearActiveResources();
            RelationContext.Clear();
        });

        btnGroup.Button("em_ui_test_generation".lang(), () => {
            LayerEmmersivePanel.Instance!.OnLayoutConfirm();
            EmScheduler.RequestScenePlayImmediate();
            ELayer.ui.RemoveLayer<LayerEmmersivePanel>();
        });

        btnGroup.Button("em_ui_open_debug".lang(), () => {
            EmConfig.Policy.Verbose.Value = true;
            var path = PackageIterator.GetRelocatedDirFromPackage("Emmersive", ModInfo.Guid);
            // TODO: use CWL version after updating
            OpenFileOrPath.Run(path!.FullName);
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

        AddServiceButton("em_ui_add_service_google".lang(), apiKey => new GoogleProvider(apiKey));
        AddServiceButton("em_ui_add_service_openai".lang(), apiKey => new OpenAIProvider(apiKey));

        return;

        void AddServiceButton(string btnName, Func<string, IChatProvider> serviceFactory)
        {
            btnGroup.Button(btnName, () => {
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
            });
        }
    }

    private static void AddService(IChatProvider provider)
    {
        ApiPoolSelector.Instance.AddService(provider);
        EmKernel.RebuildKernel();
        LayerEmmersivePanel.Instance?.Reopen();
    }
}