using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib.Public.Patching;

namespace Cwl.Helper.Runtime.Exceptions;

public class ExceptionProfile(string stackTrace)
{
    private static readonly Dictionary<int, ExceptionProfile> _cached = [];

    public bool Analyzing { get; private set; }
    public bool Analyzed { get; private set; }
    public int Occurrences { get; private set; } = 1;
    public string Result { get; private set; } = "cwl_ui_exception_analyzing".Loc();
    public int Key { get; private set; }

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
        return GetFromStackTrace(Regex.Replace(exception.StackTrace, @"^(\s+at\s)", ""));
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

    private IEnumerator DeferredAnalyzer()
    {
        var sb = new StringBuilder(stackTrace.Length);
        var lineCount = 0;

        foreach (var frame in stackTrace.SplitNewline()) {
            var mono = MonoFrame.GetFrame(frame).Parse();
            switch (mono.frameType) {
                case MonoFrame.StackFrameType.Unknown:
                case MonoFrame.StackFrameType.Rethrow:
                    continue;
                case MonoFrame.StackFrameType.Method or MonoFrame.StackFrameType.DynamicMethod:
                    try {
                        var info = mono.Method.GetPatchInfo();

                        if (lineCount <= 15 || info is not null) {
                            sb.AppendLine(mono.Method!.GetDetail());
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
        Analyzed = true;

        CwlMod.Log<ExceptionProfile>(Result);
    }
}