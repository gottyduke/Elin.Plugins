using System;
using Emmersive.API;
using Emmersive.API.Services;
using Emmersive.Components;
using Emmersive.Helper;
using UnityEngine;
using UnityEngine.UI;
using YKF;
using Object = UnityEngine.Object;

namespace Emmersive.ChatProviders;

public abstract partial class ChatProviderBase : ILayoutProvider
{
    private UIInputText? _aliasInput;
    private UIInputText? _endpointInput;
    private UIInputText? _modelInput;

    public void OnLayout(YKLayout layout)
    {
        var card = layout.MakeCard();
        card.Layout.GetOrCreate<Image>().color = IsAvailable ? Color.cyan : Color.red;

        var header = card.Horizontal();
        header.Layout.childForceExpandWidth = true;

        var serviceId = header.Text(Id, IsAvailable ? FontColor.Good : FontColor.Bad);
        serviceId.fontSize *= 2;

        var btn = header.Button("em_ui_reload".lang(), () => {
            OnLayoutConfirm();
            LayerEmmersivePanel.Instance?.Reopen();
        }).GetComponent<Image>();
        btn.color = Color.yellow;

        card.Spacer(5);

        if (!IsAvailable && !UnavailableReason.IsEmpty()) {
            card.TextLong(UnavailableReason!);
            card.Spacer(15);
        }

        card.ShowActivityInfo(Id);

        _modelInput = card.AddPair("em_ui_model", CurrentModel);
        _endpointInput = card.AddPair("em_ui_endpoint", EndPoint);
        _aliasInput = card.AddPair("em_ui_alias", Alias);

        OnLayoutInternal(card);

        var controlGroup = card.Horizontal();
        controlGroup.Layout.childForceExpandWidth = true;

        controlGroup.Button("em_ui_edit_params".lang(), this.OpenProviderParam);

        controlGroup.Button("em_ui_remove".lang(), () => {
            ApiPoolSelector.Instance.RemoveService(this);
            EmKernel.RebuildKernel();
            Object.DestroyImmediate(card.transform.parent.gameObject);
        }).GetOrCreate<Image>().color = Color.red;
    }

    public virtual void OnLayoutConfirm()
    {
        if (_modelInput is not null) {
            CurrentModel = _modelInput.Text;
        }

        if (_endpointInput != null) {
            EndPoint = _endpointInput.Text;
        }

        if (_aliasInput != null) {
            Id = Id.Replace(Alias, _aliasInput.Text);
            Alias = _aliasInput.Text;
        }

        _cooldownUntil = DateTime.MinValue;

        this.LoadProviderParam();

        EmKernel.RebuildKernel();
    }

    protected abstract void OnLayoutInternal(YKLayout card);
}