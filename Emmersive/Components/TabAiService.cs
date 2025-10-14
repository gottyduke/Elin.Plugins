using Emmersive.API;
using Emmersive.API.Services;
using Emmersive.ChatProviders;
using YKF;

namespace Emmersive.Components;

internal class TabAiService : TabEmmersiveBase
{
    private YKVertical? _cards;

    public override void OnLayout()
    {
        BuildButtons();

        _cards = Vertical();
        _cards.Layout.childForceExpandWidth = true;

        foreach (var provider in ApiPoolSelector.Instance.Providers) {
            provider.OnLayout(_cards);
        }
    }

    public void BuildButtons()
    {
        var btnGroup = Horizontal()
            .WithSpace(10);

        btnGroup.Layout.childForceExpandWidth = true;

        btnGroup.Button("em_ui_add_service_google", () => {
            Dialog.InputName(
                "Paste Your API Key Here",
                "API Key",
                (cancel, apiKey) => {
                    if (!cancel) {
                        AddService(new GoogleProvider(apiKey));
                    }
                },
                Dialog.InputType.Password);
        });

        btnGroup.Button("em_ui_add_service_openai", () => {
            Dialog.InputName(
                "API Key",
                "Paste API Key Here",
                (cancel, apiKey) => {
                    if (!cancel) {
                        AddService(new OpenAIProvider(apiKey));
                    }
                },
                Dialog.InputType.Password);
        });
    }

    private void AddService(IChatProvider provider)
    {
        ApiPoolSelector.Instance.AddService(provider);
        EmKernel.RebuildKernel();
        provider.OnLayout(_cards!);
    }
}