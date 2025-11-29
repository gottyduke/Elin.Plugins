using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using UnityEngine;

namespace Cwl.Helper.Exceptions;

public class ExceptionProfile(string message)
{
    public enum AnalyzeState
    {
        NotStarted,
        InProgress,
        Completed,
    }

    private static readonly Dictionary<int, ExceptionProfile> _cached = [];
    private ProgressIndicator? _progressIndicator;

    public List<MonoFrame> Frames => field ??= [];

    public AnalyzeState State { get; private set; } = AnalyzeState.NotStarted;

    public int Occurrences { get; private set; } = 1;
    public string Result { get; private set; } = "cwl_ui_exception_analyzing".lang();
    public int Key { get; private init; }
    public bool Hidden { get; private set; }
    public bool IsMissingMethod { get; private set; }
    public string StackTrace { get; private init; } = "";

    public static ExceptionProfile GetFromStackTrace(string stackTrace, string message)
    {
        var hash = stackTrace.GetHashCode();
        if (!_cached.TryGetValue(hash, out var profile)) {
            profile = _cached[hash] = new(message) {
                StackTrace = stackTrace,
                Key = hash,
            };
        } else {
            profile.Occurrences++;
        }

        return profile;
    }

    public static ExceptionProfile GetFromStackTrace(ref Exception exception)
    {
        while (exception.InnerException is { } inner and not SourceParseException) {
            exception = inner;
        }

        var exp = GetFromStackTrace(Regex.Replace(exception.StackTrace.IsEmpty(""), @"^(\s+at\s)", ""), exception.Message);

        var stackTrace = new StackTrace(exception);
        foreach (var frame in stackTrace.GetFrames() ?? []) {
            var method = frame.GetMethod();
            exp.Frames.Add(method is not null
                ? MonoFrame.GetFrame(method)
                : MonoFrame.GetFrame(frame.GetFieldValue("internalMethodName") as string));
        }

        return exp;
    }

    [Obsolete("use ref overload for inner exception swap")]
    public static ExceptionProfile GetFromStackTrace(Exception exception)
    {
        return GetFromStackTrace(ref exception);
    }

    public static void DefaultExceptionHandler(Exception exception)
    {
        var profile = GetFromStackTrace(ref exception);
        profile.CreateAndPop();
    }

    public void CreateAndPop(string? display = null)
    {
        EMono.ui?.hud?.imageCover?.SetActive(false);

        if (_progressIndicator is { IsKilled: false }) {
            _progressIndicator.ResetDuration();
            return;
        }

        // missing method exception
        display ??= message;
        IsMissingMethod = display.Contains(nameof(MissingMethodException));
        if (IsMissingMethod) {
            display = display.Replace(nameof(MissingMethodException),
                "cwl_warn_missing_method".Loc(nameof(MissingMethodException)));
        }

        using var scopeExit =
            ProgressIndicator.CreateProgressScoped(() => new(GetOccurrenceString() + display, Color: Color.red));
        _progressIndicator = scopeExit.Get<ProgressIndicator>();

        if (!CwlConfig.ExceptionAnalyze) {
            return;
        }

        _progressIndicator
            .OnHover(p => {
                Analyze();

                GUILayout.Label($"{"cwl_ui_exception_copy".lang()}\n{Result.SplitLines()
                    .Take(20)
                    .Join(r => r, Environment.NewLine)}", p.GUIStyle);

                if (State is AnalyzeState.InProgress) {
                    GUILayout.Label("cwl_ui_exception_analyzing".lang(), p.GUIStyle);
                }
            })
            .OnEvent(ClickHandler);
    }

    public void Analyze()
    {
        if (State is not AnalyzeState.NotStarted) {
            return;
        }

        State = AnalyzeState.InProgress;

        DeferredAnalyzer().Forget();
    }

    private void ClickHandler(ProgressIndicator progress, Event eventData)
    {
        switch (eventData.button) {
            case 0 when State is AnalyzeState.Completed:
                GUIUtility.systemCopyBuffer = $"{message}\n```ts\n{Result}\n```".RemoveTagColor();
                break;
            case 2:
                Hidden = true;
                progress.Kill();
                eventData.Use();
                break;
        }
    }

    private string GetOccurrenceString()
    {
        if (Occurrences == 1) {
            return "";
        }

        var text = Occurrences <= 999 ? Occurrences.ToString() : "999+";
        return $"<color=black><b>({text})</b></color> ";
    }

    private async UniTaskVoid DeferredAnalyzer()
    {
        await UniTask.SwitchToThreadPool();

        try {
            Frames.Clear();

            using var sb = StringBuilderPool.Get();
            sb.AppendLine("cwl_ui_callstack".lang());

            foreach (var frame in StackTrace.SplitLines().Distinct()) {
                if (frame.IsEmpty()) {
                    continue;
                }

                var mono = MonoFrame.GetFrame(frame).Parse();
                if (Frames.Find(f => f.DetailedMethodCall == mono.DetailedMethodCall) is not null) {
                    continue;
                }

                Frames.Add(mono);

                switch (mono.FrameType) {
                    case MonoFrame.StackFrameType.Rethrow:
                        sb.AppendLine("---");
                        break;
                    case MonoFrame.StackFrameType.Unknown:
                        sb.AppendLine(mono.SanitizedMethodCall.ToTruncateString(150));
                        break;
                    case MonoFrame.StackFrameType.Method or MonoFrame.StackFrameType.DynamicMethod:
                        try {
                            if (IsMissingMethod && !mono.IsVendorMethod) {
                                mono.Method!.TestIncompatibleIl();
                            }

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
            }

            Result = sb.ToString();
            CwlMod.Log<ExceptionProfile>(Result);

            Result = Result.TruncateAllLines(150);

            State = AnalyzeState.Completed;
        } finally {
            await UniTask.Yield();
        }
    }
}