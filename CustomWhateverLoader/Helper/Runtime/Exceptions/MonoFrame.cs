using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Cwl.Helper.String;

namespace Cwl.Helper.Exceptions;

public class MonoFrame
{
    public enum StackFrameType
    {
        Unknown,
        Method,
        Rethrow,
        DynamicMethod,
    }

    private static readonly Dictionary<string, MonoFrame> _cached = [];

    private static readonly HashSet<string> _vendorExclusion = new(StringComparer.Ordinal) {
        "Elin.",
        "UnityEngine.",
        "Plugins.",
        "System.",
        "mscorlib.",
        "MonoMod.",
        "0Harmony.",
    };

    private MonoFrame(string stackFrame)
    {
        StackFrame = stackFrame;
    }

    public StackFrameType FrameType { get; private set; } = StackFrameType.Unknown;

    public bool Parsed { get; private set; }

    public MethodBase? Method { get; private set; }
    public string StackFrame { get; }
    public string SanitizedMethodCall { get; private set; } = "";
    public (string? type, string? name)[] SanitizedParameters { get; private set; } = [];

    public string DetailedMethodCall =>
        FrameType is StackFrameType.Method or StackFrameType.DynamicMethod
            ? IsVendorMethod ? Method!.GetDetail(false) : Method!.GetAssemblyDetailColor(false)
            : SanitizedMethodCall;

    public string? AssemblyName => field ??= Method?.Module.ToString();
    public bool IsVendorMethod => _vendorExclusion.Any(AssemblyName.IsEmpty("").StartsWith);

    public static MonoFrame GetFrame(string frame)
    {
        if (!_cached.TryGetValue(frame, out var profile)) {
            profile = _cached[frame] = new(frame);
        }

        return profile;
    }

    public static MonoFrame GetFrame(MethodBase method)
    {
        var frame = method.ToString();
        if (!_cached.TryGetValue(frame, out var profile)) {
            profile = _cached[frame] = new(frame) {
                Parsed = true,
                Method = method,
                FrameType = StackFrameType.Method,
            };
        }

        return profile;
    }

    public static bool HasFrame(string frame)
    {
        return _cached.ContainsKey(frame);
    }

    public static void AddVendorExclusion(string assemblyName)
    {
        _vendorExclusion.Add(assemblyName);
    }

    [SwallowExceptions]
    public MonoFrame Parse()
    {
        if (Parsed) {
            return this;
        }

        var raw = StackFrame.Trim();
        SanitizeFrame();

        // explicit dmd, discard mono jit
        if (raw.StartsWith("Rethrow as")) {
            FrameType = StackFrameType.Rethrow;
        } else {
            FrameType = TryGetMethodCallParts(SanitizedMethodCall, out var typeName, out var methodName);
            if (FrameType is StackFrameType.Method or StackFrameType.DynamicMethod) {
                Method = CachedMethods.GetCachedMethod(typeName, methodName, SanitizedParameters);

                if (Method is null && FrameType == StackFrameType.DynamicMethod && SanitizedParameters.Length >= 1) {
                    SanitizedParameters = SanitizedParameters[1..];
                    Method = CachedMethods.GetCachedMethod(typeName, methodName, SanitizedParameters);
                }

                if (Method is null) {
                    FrameType = StackFrameType.Unknown;
                }
            }
        }

        Parsed = true;
        return this;
    }

    public string[] SanitizeFrame()
    {
        var raw = Regex.Replace(StackFrame.Trim(), @"\(wrapper[^\)]*\)\s|^at", "").Trim();
        var parts = raw.Split('(', 2, StringSplitOptions.RemoveEmptyEntries);

        SanitizedMethodCall = parts[0].Trim();

        if (parts.Length < 2) {
            return parts;
        }

        var seg = parts[1].LastIndexOf(')');
        if (seg != -1) {
            parts[1] = parts[1][..seg];
        }

        SanitizedParameters = SplitParameters(parts[1].Trim('(', ')', ' '));

        return parts;
    }

    public static StackFrameType TryGetMethodCallParts(string partialMethodCall, out string typeName, out string methodName)
    {
        typeName = methodName = "";

        var match = Regex.Match(partialMethodCall, "DMD<([^>]+)>");
        if (match.Success) {
            var fullName = match.Groups[1].Value;
            var nameParts = fullName.Split("::", 2);
            if (nameParts.Length == 2) {
                typeName = nameParts[0];
                methodName = nameParts[1];
                return StackFrameType.DynamicMethod;
            }
        }

        var lastDotIndex = partialMethodCall.LastIndexOf('.');
        if (lastDotIndex != -1) {
            typeName = partialMethodCall[..lastDotIndex];
            methodName = partialMethodCall[(lastDotIndex + 1)..];

            return StackFrameType.Method;
        }

        return StackFrameType.Unknown;
    }

    public static (string? type, string? name)[] SplitParameters(string input)
    {
        var segments = new List<(string?, string?)>();
        using var sb = StringBuilderPool.Get();
        var current = sb.StringBuilder;
        var depth = 0;

        foreach (var c in input) {
            switch (c) {
                case '<':
                case '[':
                    depth++;
                    current.Append(c);
                    break;
                case '>':
                case ']':
                    depth--;
                    current.Append(c);
                    break;
                case ',' when depth == 0:
                    segments.Add(SplitParam());
                    current.Clear();
                    break;
                default:
                    current.Append(c);
                    break;
            }
        }

        if (current.Length > 0) {
            segments.Add(SplitParam());
        }

        return segments.ToArray();

        (string, string) SplitParam()
        {
            var param = current.ToString().Trim().Split(' ');
            return (param[0], param.TryGet(1, true));
        }
    }
}