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
        btn.color = Color.yellow;

        var cardBg = card.Layout.gameObject.AddComponent<Image>();
        cardBg.sprite = btn.sprite;
        cardBg.type = Image.Type.Sliced;
        cardBg.color = IsAvailable ? Color.cyan : Color.red;

        if (!IsAvailable && !UnavailableReason.IsEmpty()) {
            card.TextLong(UnavailableReason!);
        }

        ShowActivityInfo(card);

        _modelInput = card.AddPair("em_ui_model".lang(), CurrentModel);

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

        _cooldownUntil = DateTime.MinValue;

        this.LoadProviderParam();

        EmKernel.RebuildKernel();
    }

    private void ShowActivityInfo(YKLayout layout)
    {
        var summary = EmActivity.GetSummary(Id);
        if (summary.RequestTotal == 0) {
            return;
        }

        var card = layout.Horizontal();
        card.Layout.childForceExpandWidth = true;

        var left = card.Vertical();
        left.TopicDomain("em_ui_requests_total", $"{summary.RequestTotal:N0}");
        left.TopicDomain("em_ui_requests_success", $"{summary.RequestSuccess:N0}");
        left.TopicDomain("em_ui_requests_failed", $"{summary.RequestFailure:N0}");
        left.TopicDomain("em_ui_requests_rph", $"{summary.RequestLastHour:N0}");
        left.TopicDomain("em_ui_requests_rpm", $"{summary.RequestSuccessPerMin:N0}");

        var right = card.Vertical();
        right.TopicDomain("em_ui_tokens_total", $"{summary.TokensTotal:N0}");
        right.TopicDomain("em_ui_tokens_input", $"{summary.TokensInput:N0}");
        right.TopicDomain("em_ui_avg_latency", $"{summary.AverageLatency:N1}s");
        right.TopicDomain("em_ui_tokens_tph", $"{summary.TokensLastHour:N0}");
        right.TopicDomain("em_ui_tokens_tpm", $"{summary.TokensPerMin:N1}");
    }

    protected abstract void OnLayoutInternal(YKLayout card);
}