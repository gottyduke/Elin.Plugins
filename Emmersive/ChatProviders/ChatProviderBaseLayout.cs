using Emmersive.API;
using Emmersive.API.Services;
using Emmersive.Components;
using Emmersive.Helper;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Emmersive.ChatProviders;

public abstract partial class ChatProviderBase : ILayoutProvider
{
    public void OnLayout(YKLayout layout)
    {
        var card = layout.Vertical();
        card.LayoutElement().flexibleWidth = 1f;
        card.Fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        card.Layout.padding = new(20, 20, 20, 20);

        var header = card.Horizontal();
        header.Layout.childForceExpandWidth = true;

        var serviceId = header.Text(Id, IsAvailable ? FontColor.Good : FontColor.Bad);
        serviceId.fontSize *= 2;

        var btn = header.Button("em_ui_reload".lang(), () => {
            OnLayoutConfirm();
            LayerEmmersivePanel.Instance?.Reopen();
        }).GetComponent<Image>();
        btn.color = Color.red;

        var cardBg = card.Layout.gameObject.AddComponent<Image>();
        cardBg.sprite = btn.sprite;
        cardBg.type = Image.Type.Sliced;
        cardBg.color = IsAvailable ? Color.cyan : Color.red;

        if (!IsAvailable && !UnavailableReason.IsEmpty()) {
            card.TextLong(UnavailableReason!);
        }

        _modelInput = card.AddPair("em_ui_model".lang(), CurrentModel);

        OnLayoutInternal(card);

        var controlGroup = card.Horizontal();
        controlGroup.Layout.childForceExpandWidth = true;

        controlGroup.Button("em_ui_edit_params".lang(), this.OpenProviderParam);

        controlGroup.Button("em_ui_remove".lang(), () => {
            ApiPoolSelector.Instance.RemoveService(this);
            EmKernel.RebuildKernel();
            Object.DestroyImmediate(card.transform.parent.gameObject);
        }).GetOrCreate<Image>().color = Color.yellow;
    }

    public virtual void OnLayoutConfirm()
    {
        if (_modelInput is not null) {
            CurrentModel = _modelInput.Text;
        }

        this.LoadProviderParam();

        EmKernel.RebuildKernel();
    }

    protected abstract void OnLayoutInternal(YKLayout card);
}