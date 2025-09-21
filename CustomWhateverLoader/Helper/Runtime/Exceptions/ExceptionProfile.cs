using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib.Public.Patching;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cwl.Helper.Exceptions;

public class ExceptionProfile(string stackTrace)
{
    public enum AnalysisState
    {
        NotStarted,
        InProgress,
        Completed,
    }

    private static readonly Dictionary<int, ExceptionProfile> _cached = [];
    private static readonly Dictionary<int, ProgressIndicator> _activeExceptions = [];

    public List<MonoFrame> Frames { get; } = [];

    public AnalysisState State { get; private set; } = AnalysisState.NotStarted;

    public int Occurrences { get; private set; } = 1;
    public string Result { get; private set; } = "cwl_ui_exception_analyzing".Loc();
    public int Key { get; private set; }
    public bool Hidden { get; private set; }
    public bool IsMissingMethod { get; private set; }

    public static ExceptionProfile GetFromStackTrace(string stackTrace)
    {
        var hash = stackTrace.GetHashCode();
        if (!_cached.TryGetValue(hash, out var profile)) {
            profile = _cached[hash] = new(stackTrace);
            profile.Key = hash;
        } else {
            profile.Occurrences++;
        }

        return profile;
    }

    public static ExceptionProfile GetFromStackTrace(ref Exception exception)
    {
        while (exception.InnerException is { } inner) {
            exception = inner;
        }

        return GetFromStackTrace(Regex.Replace(exception.StackTrace.IsEmpty(""), @"^(\s+at\s)", ""));
    }

    [Obsolete("use ref overload for inner exception swap")]
    public static ExceptionProfile GetFromStackTrace(Exception exception)
    {
        return GetFromStackTrace(ref exception);
    }

    public void CreateAndPop(string message)
    {
        EMono.ui?.hud?.imageCover?.SetActive(false);

        if (_activeExceptions.TryGetValue(Key, out var progress)) {
            if (progress != null) {
                progress
                    .AppendTailText(() => Occurrences <= 999 ? Occurrences.ToString() : "999+")
                    .ResetProgress();
                return;
            }
        }

        // missing method exception
        IsMissingMethod = message.Contains(nameof(MissingMethodException));
        if (IsMissingMethod) {
            message = message.Replace(nameof(MissingMethodException), "cwl_warn_missing_method".Loc(nameof(MissingMethodException)));
        }

        using var scopeExit = ProgressIndicator.CreateProgressScoped(() => new(message, Color: Color.red));
        progress = _activeExceptions[Key] = scopeExit.Get<ProgressIndicator>();

        if (!CwlConfig.ExceptionAnalyze) {
            return;
        }

        progress
            .AppendHoverText(() => {
                StartAnalyzing();
                return Result.Truncate(2048);
            })
            .SetHoverPrompt("cwl_ui_exception_analyzing".Loc(), "cwl_ui_exception_analyze".Loc())
            .SetClickHandler(ClickHandler);

        CoroutineHelper.Deferred(
            () => {
                if (progress == null) {
                    return;
                }

                progress.SetHoverPrompt();
            },
            () => progress == null || State == AnalysisState.Completed);
    }

    public void StartAnalyzing(bool deferred = true)
    {
        if (State != AnalysisState.NotStarted) {
            return;
        }

        State = AnalysisState.InProgress;

        if (deferred) {
            CoroutineHelper.Immediate(DeferredAnalyzer());
        } else {
            var enumerator = DeferredAnalyzer();
            while (enumerator.MoveNext()) {
            }
        }
    }

    private void ClickHandler(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Middle) {
            return;
        }

        Hidden = true;
        _activeExceptions[Key].Kill();
    }

    private IEnumerator DeferredAnalyzer()
    {
        Frames.Clear();

        using var sb = StringBuilderPool.Get();
        sb.AppendLine("cwl_ui_callstack".Loc());

        var terminated = false;
        foreach (var frame in stackTrace.SplitLines().Distinct()) {
            if (terminated) {
                break;
            }

            if (frame.IsEmpty()) {
                continue;
            }

            var mono = MonoFrame.GetFrame(frame).Parse();
            Frames.Add(mono);

            switch (mono.frameType) {
                case MonoFrame.StackFrameType.Rethrow:
                    terminated = true;
                    break;
                case MonoFrame.StackFrameType.Unknown:
                    sb.AppendLine(mono.SanitizedMethodCall.ToTruncateString(150));
                    break;
                case MonoFrame.StackFrameType.Method or MonoFrame.StackFrameType.DynamicMethod:
                    try {
                        sb.AppendLine(mono.DetailedMethodCall);
                        var info = mono.Method.GetPatchInfo();
                        if (info is not null) {
                            if (IsMissingMethod) {
                                info.TestIncompatiblePatch();
                            }

                            info.DumpPatchDetails(sb.StringBuilder);
                        }
                    } catch {
                        // noexcept
                    }

                    break;
            }

            yield return null;
        }

        Result = sb.ToString();
        CwlMod.Log<ExceptionProfile>(Result);
        Result = Result.TruncateAllLines(150);

        State = AnalysisState.Completed;
    }
}