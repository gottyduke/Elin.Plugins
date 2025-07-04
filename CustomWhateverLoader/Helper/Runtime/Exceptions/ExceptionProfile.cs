using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib.Public.Patching;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cwl.Helper.Exceptions;

public class ExceptionProfile(string stackTrace)
{
    private static readonly Dictionary<int, ExceptionProfile> _cached = [];
    private static readonly Dictionary<int, ProgressIndicator> _activeExceptions = [];

    public List<MonoFrame> Frames { get; } = [];
    public bool Analyzing { get; private set; }
    public bool Analyzed { get; private set; }
    public int Occurrences { get; private set; } = 1;
    public string Result { get; private set; } = "cwl_ui_exception_analyzing".Loc();
    public int Key { get; private set; }
    public bool Hidden { get; private set; }

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

    public static ExceptionProfile GetFromStackTrace(Exception exception)
    {
        return GetFromStackTrace(Regex.Replace(exception.StackTrace.IsEmpty(""), @"^(\s+at\s)", ""));
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

        using var scopeExit = ProgressIndicator.CreateProgressScoped(() => new(message, Color: Color.red));
        progress = _activeExceptions[Key] = scopeExit.Get<ProgressIndicator>();

        if (!CwlConfig.ExceptionAnalyze) {
            return;
        }

        progress
            .AppendHoverText(() => {
                StartAnalyzing();
                return Result;
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
            () => progress == null || Analyzed);
    }

    public void StartAnalyzing(bool deferred = true)
    {
        if (Analyzing) {
            return;
        }

        Analyzing = true;

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

        var sb = new StringBuilder(stackTrace.Length);
        var lineCount = 0;

        foreach (var frame in stackTrace.SplitLines()) {
            if (frame.IsEmpty()) {
                // skip reverse patch stubs
                continue;
            }

            var mono = MonoFrame.GetFrame(frame).Parse();
            Frames.Add(mono);

            switch (mono.frameType) {
                case MonoFrame.StackFrameType.Unknown or MonoFrame.StackFrameType.Rethrow:
                    sb.AppendLine(mono.SanitizedMethodCall.ToTruncateString(150));
                    break;
                case MonoFrame.StackFrameType.Method or MonoFrame.StackFrameType.DynamicMethod:
                    try {
                        var info = mono.Method.GetPatchInfo();

                        if (lineCount <= 15 || info is not null) {
                            sb.AppendLine(mono.DetailedMethodCall);
                            lineCount++;
                        }

                        if (info is null) {
                            continue;
                        }

                        sb.AppendPatchInfo(info);
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
        Analyzed = true;
    }

    private static Exception? Ignore(Exception? __exception)
    {
        return null;
    }
}