using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Emmersive.API;
using Emmersive.API.Services;
using Emmersive.API.ThirdParty;
using Emmersive.Components;
using Emmersive.Helper;
using Emmersive.LangMod;
using UnityEngine;
using UnityEngine.UI;
using YKF;
using Object = UnityEngine.Object;

namespace Emmersive.ChatProviders;

public abstract partial class ChatProviderBase : ILayoutProvider
{
    private UIInputText? _aliasInput;
    private Image? _cardImage;
    private UIInputText? _endpointInput;
    private UIText? _hintText;
    private UIText? _idText;
    private UIInputText? _modelInput;
    private UIText? _statsText;
    private UIButton? _testButton;
    private UIText? _testResult;
    private bool _testing;

    public virtual bool AllowEndpointCustomization { get; protected set; } = true;
    public virtual bool AllowModelCustomization { get; protected set; } = true;

    private string? StatusHint =>
        UnavailableReason ??
        (RequiresApiKey && ApiKey.IsEmptyOrNull ? "em_ui_err_no_key".lang() :
        !IsConfigured ? "em_ui_err_not_configured".lang() : null);

    public void OnLayout(YKLayout layout)
    {
        var card = layout.MakeCard();
        _cardImage = card.Layout.GetOrCreate<Image>();

        var header = card.Horizontal();
        header.Layout.childForceExpandWidth = true;

        _idText = header.Text(Alias);

        header.Button("em_ui_reload".lang(), () => LayerEmmersivePanel.Instance?.Reopen())
            .GetComponent<Image>().color = Color.yellow;

        card.Spacer(5);

        _hintText = null;
        if (!IsAvailable && StatusHint is { } hint) {
            _hintText = card.TextLong(hint);
            card.Spacer(15);
        }

        _statsText = card.Text("");
        RefreshStatus();

        if (AllowModelCustomization) {
            _modelInput = card.AddPair("em_ui_model", CurrentModel);
        }

        if (AllowEndpointCustomization) {
            _endpointInput = card.AddPair("em_ui_endpoint", EndPoint);
        }

        _aliasInput = card.AddPair("em_ui_alias", Alias);

        OnLayoutInternal(card);

        _testResult = card.Text("");
        _testResult.SetActive(false);

        var controlGroup = card.Horizontal();
        controlGroup.Layout.childForceExpandWidth = true;

        _testButton = controlGroup.Button("em_ui_test_connection".lang(), () => TestConnectionAsync().ForgetEx());

        if (RequiresApiKey) {
            controlGroup.Button("em_ui_change_key".lang(), () => AskApiKey(Alias, key => {
                ApplyApiKey(key);
                LayerEmmersivePanel.Instance?.Reopen();
            }));
        }

        controlGroup.Button("em_ui_edit_params".lang(), this.OpenProviderParam);

        controlGroup.Button("em_ui_remove".lang(), () => UIHelper.ConfirmDanger("em_ui_confirm_remove_service", () => {
            ApiPoolSelector.Instance.RemoveService(this);
            EmKernel.RebuildKernel();
            Object.DestroyImmediate(card.transform.parent.gameObject);
        }, "em_ui_remove")).GetOrCreate<Image>().color = Color.red;
    }

    public virtual void OnLayoutConfirm()
    {
        if (_modelInput != null) {
            var model = _modelInput.Text.Trim();
            if (model.IsEmptyOrNull) {
                _modelInput.Text = CurrentModel;
            } else {
                CurrentModel = model;
            }
        }

        if (_endpointInput != null) {
            var endpoint = _endpointInput.Text.Trim();
            if (IsHttpUrl(endpoint)) {
                EndPoint = endpoint;
            } else {
                if (endpoint != EndPoint) {
                    EmMod.WarnWithPopup<IChatProvider>($"[{Alias}] {"em_ui_err_endpoint".lang()}");
                }

                _endpointInput.Text = EndPoint;
            }
        }

        if (_aliasInput != null) {
            var alias = _aliasInput.Text.Trim();
            if (alias.IsEmptyOrNull || alias.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) {
                if (alias != Alias) {
                    EmMod.WarnWithPopup<IChatProvider>($"[{Alias}] {"em_ui_err_alias".lang()}");
                }

                _aliasInput.Text = Alias;
            } else if (alias != Alias) {
                Rename(alias);
            }
        }

        ResetAvailability();

        this.LoadProviderParam();
    }

    internal static void AskApiKey(string name, Action<string> onKey)
    {
        var d = Dialog.InputName(
            $"{name} · {"em_ui_paste_api_key".lang()}",
            "",
            (cancel, key) => {
                key = key.Trim();
                if (!cancel && !key.IsEmptyOrNull) {
                    onKey(key);
                }
            });
        d.input.field.characterLimit = 512;
        d.input.field.contentType = InputField.ContentType.Password;
        d.input.field.text = "";
        UIHelper.SetPlaceholder(d.input.field, "em_ui_api_key".lang());
    }

    internal void ApplyApiKey(string key)
    {
        ApiKey = key;
        _undecryptedKey = null;
        ResetAvailability();
    }

    protected abstract void OnLayoutInternal(YKLayout card);

    private void ResetAvailability()
    {
        _cooldownUntil = DateTime.MinValue;
        _timeoutIncremental = 0f;
        UnavailableReason = null;
    }

    private void RefreshStatus()
    {
        var available = IsAvailable;
        _cardImage?.color = available ? Color.cyan : Color.red;

        if (_idText != null) {
            _idText.SetText(Alias, available ? FontColor.Good : FontColor.Bad);
            _idText.fontSize *= 2;
        }

        if (_statsText == null) {
            return;
        }

        var summary = EmActivity.GetSummary(Id);
        _statsText.SetText("em_ui_stats_summary".Loc(
            summary.RequestTotal.ToString("N0"),
            summary.RequestFailure.ToString("N0"),
            summary.LatencyAverage.ToString("N1"),
            summary.TokensTotal.ToString("N0")));
        _statsText.SetActive(summary.RequestTotal > 0);
    }

    private async UniTask TestConnectionAsync()
    {
        var button = _testButton;
        var result = _testResult;
        if (button == null || result == null) {
            return;
        }

        button.SetInteractableWithAlpha(false);
        button.mainText.text = "em_ui_testing".lang();

        OnLayoutConfirm();
        if (_idText != null) {
            _idText.text = Alias;
        }

        RequestReport report;
        _testing = true;
        try {
            EmKernel.RebuildKernel();
            report = await EmAi.SendWithReportAsync("Connection test, reply with an empty JSON array.", "ping", Id,
                UniTasklet.GameToken, false);
        } finally {
            _testing = false;
        }

        if (button == null || result == null) {
            return;
        }

        button.SetInteractableWithAlpha(true);
        button.mainText.text = "em_ui_test_connection".lang();

        if (report.Success) {
            ResetAvailability();
        } else {
            EmMod.Warn<ChatProviderBase>($"[{Id}] connection test failed: {report.Status}, {report.ErrorReason}");
        }

        _hintText?.SetActive(false);
        RefreshStatus();

        result.SetActive(true);
        result.SetText(report.Status switch {
            RequestStatus.Success => "em_ui_test_ok".Loc(report.LatencyMs.ToString("F0"),
                report.TokensInput + report.TokensOutput),
            RequestStatus.Unavailable => StatusHint ?? "em_ui_err_not_configured".lang(),
            _ => report.LocalizedError ?? "em_ui_err_unknown".lang(),
        }, report.Success ? FontColor.Good : FontColor.Bad);
    }

    private void Rename(string alias)
    {
        var oldId = Id;
        Alias = alias;
        Id = $"{alias}#{oldId[(oldId.LastIndexOf('#') + 1)..]}";

        foreach (var ext in (string[])[".txt", ".json"]) {
            var from = Path.Combine(ResourceFetch.CustomFolder, $"Params/{oldId}{ext}");
            var to = Path.Combine(ResourceFetch.CustomFolder, $"Params/{Id}{ext}");
            if (!File.Exists(from)) {
                continue;
            }

            try {
                if (File.Exists(to)) {
                    File.Delete(to);
                }

                File.Move(from, to);
            } catch (Exception ex) {
                EmMod.Warn<IChatProvider>($"[{Id}] failed to move params file {from}: {ex.Message}");
            }
        }
    }

    private static bool IsHttpUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
    }
}