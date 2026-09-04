using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Logger = HarmonyLib.Tools.Logger;

namespace EModding.Helper.Runtime.Exceptions;

public class ExceptionProfile(string message)
{
    public enum AnalyzeState
    {
        NotStarted,
        InProgress,
        Completed,
    }

    private static readonly Dictionary<int, ExceptionProfile> _cached = [];
    private static readonly string[] _missingMemberType = [
        nameof(MissingMethodException),
        nameof(MissingFieldException),
        nameof(MissingMemberException),
    ];
    private EGui? _gui;

    private StackFrame[]? _runtimeFrames;

    public List<MonoFrame> Frames => field ??= [];

    public AnalyzeState State { get; private set; } = AnalyzeState.NotStarted;

    public int Occurrences { get; private set; } = 1;
    public string Result { get; private set; } = "es_ui_exception_analyzing".lang();
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

        profile._runtimeFrames ??= CaptureFrames(ExceptionRelay.Take(stackTrace, message));

        return profile;
    }

    public static ExceptionProfile GetFromStackTrace(ref Exception exception)
    {
        while (exception.InnerException is { } inner and not SourceParseException) {
            exception = inner;
        }

        var exp = GetFromStackTrace(Regex.Replace(exception.StackTrace.IsEmpty(""), @"^(\s+at\s)", ""), exception.Message);
        exp._runtimeFrames = CaptureFrames(exception) ?? exp._runtimeFrames;

        return exp;
    }

    private static StackFrame[]? CaptureFrames(Exception? exception)
    {
        if (exception is null) {
            return null;
        }

        try {
            return new StackTrace(exception).GetFrames();
        } catch {
            return null;
            // noexcept
        }
    }

    public static void DefaultExceptionHandler(Exception exception)
    {
        var profile = GetFromStackTrace(ref exception);
        profile.CreateAndPop();
    }

    public void CreateAndPop(string? display = null)
    {
        EMono.ui?.hud?.imageCover?.SetActive(false);

        if (_gui is { IsKilled: false }) {
            _gui.ResetDuration();
            return;
        }

        // missing method exception
        display ??= message;
        IsMissingMethod = _missingMemberType.Any(display.Contains);
        if (IsMissingMethod) {
            display = _missingMemberType.Aggregate(display, (current, missingMember) =>
                current.Replace(missingMember, "es_warn_missing_method".lang(missingMember)));
        }

        using var scopeExit =
            EGui.CreatePopupScoped(() => new(GetOccurrenceString() + display, Color: Color.red));
        _gui = scopeExit.Object;

        _gui
            .OnHover(p => {
                Analyze();

                GUILayout.Label($"{"es_ui_exception_copy".lang()}\n{Result.SplitByNewline()
                    .Take(20)
                    .Join(r => r, Environment.NewLine)}", p.GUIStyle);

                if (State is AnalyzeState.InProgress) {
                    GUILayout.Label("es_ui_exception_analyzing".lang(), p.GUIStyle);
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

        EClass.core.StartCoroutine(DeferredAnalyzer());
    }

    private void ClickHandler(EGui gui, Event eventData)
    {
        if (eventData.type is not EventType.MouseDown) {
            return;
        }

        switch (eventData.button) {
            case 0 when State is AnalyzeState.Completed:
                GUIUtility.systemCopyBuffer = $"{message}\n```ts\n{Result}\n```".RemoveAllTags();
                break;
            case 2:
                Hidden = true;
                gui.Kill();
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

    private IEnumerator DeferredAnalyzer()
    {
        var oldFilter = Logger.ChannelFilter;
        Logger.ChannelFilter = Logger.LogChannel.None;

        try {
            Frames.Clear();

            var sb = new StringBuilder();
            sb.AppendLine("es_ui_callstack".lang());

            if (_runtimeFrames is { Length: > 0 } runtimeFrames) {
                foreach (var frame in runtimeFrames) {
                    try {
                        AnalyzeFrame(MonoFrame.GetFrame(frame), sb);
                    } catch {
                        // noexcept
                    }
                }
            } else {
                foreach (var frame in StackTrace.SplitByNewline().Distinct()) {
                    if (string.IsNullOrEmpty(frame)) {
                        continue;
                    }

                    try {
                        AnalyzeFrame(MonoFrame.GetFrame(frame).Parse(), sb);
                    } catch {
                        // noexcept
                    }
                }
            }

            Result = sb.ToString();
            Debug.Log(Result);

            Result = Result.TruncateAllLines(150);
        } catch {
            // noexcept
        } finally {
            State = AnalyzeState.Completed;
            Logger.ChannelFilter = oldFilter;
        }

        yield break;
    }

    private void AnalyzeFrame(MonoFrame mono, StringBuilder sb)
    {
        if (Frames.Find(f => f.DetailedMethodCall == mono.DetailedMethodCall) is not null) {
            return;
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

                        info.DumpPatchDetails(sb);
                    }
                } catch {
                    sb.AppendLine(mono.SanitizedMethodCall.ToTruncateString(150));
                    // noexcept
                }

                break;
        }
    }

    public static string ReportFrameResolver()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"mono jit lookup : {(MonoNative.Available ? "active" : "unavailable")}");
        sb.AppendLine($"exception relay : {ExceptionRelay.Status}");
        sb.AppendLine("sample frames    :");

        try {
            throw new InvalidOperationException("frame resolver");
        } catch (Exception ex) {
            foreach (var frame in new StackTrace(ex).GetFrames() ?? []) {
                var mono = MonoFrame.GetFrame(frame);
                sb.AppendLine($" {mono.Resolver,-8}{(mono.IsJitted ? "jit" : "   ")}\t{mono.DetailedMethodCall}".RemoveAllTags());
            }
        }

        return sb.ToString();
    }
}