using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Cwl.Helper.String;

namespace Cwl.Helper.Exceptions;

public class MonoFrame(string stackFrame)
{
    public enum StackFrameType
    {
        Unknown,
        Method,
        Rethrow,
        DynamicMethod,
    }

    private static readonly Dictionary<string, MonoFrame> _cached = [];

    private static readonly HashSet<string> _vendorExclusion = [
        "Elin.",
        "UnityEngine.",
        "Plugins.",
        "System.",
        "mscorlib",
        "MonoMod.",
        "0Harmony",
    ];

    public StackFrameType frameType = StackFrameType.Unknown;

    public bool Parsed { get; private set; }

    public MethodInfo? Method { get; private set; }
    public string StackFrame => stackFrame;
    public string SanitizedMethodCall { get; private set; } = "";
    public string SanitizedParameters { get; private set; } = "";
    public string DetailedMethodCall { get; private set; } = "";
    public string? AssemblyName => field ??= Method?.DeclaringType?.Assembly.ManifestModule.ScopeName;
    public bool IsVendorMethod => _vendorExclusion.Any(AssemblyName!.StartsWith);

    public static MonoFrame GetFrame(string frame)
    {
        if (!_cached.TryGetValue(frame, out var profile)) {
            profile = _cached[frame] = new(frame);
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

        var raw = stackFrame.Trim();
        SanitizeFrame();

        // explicit dmd, discard mono jit
        if (raw.StartsWith("Rethrow as")) {
            frameType = StackFrameType.Rethrow;
        } else {
            Method = ExtractMethod();
            frameType = Method is null
                ? StackFrameType.Unknown
                : raw.StartsWith("(wrapper dynamic-method)")
                    ? StackFrameType.DynamicMethod
                    : StackFrameType.Method;
        }

        DetailedMethodCall = frameType is StackFrameType.Method or StackFrameType.DynamicMethod
            ? IsVendorMethod ? Method!.GetDetail(false) : Method!.GetAssemblyDetailColor(false)
            : SanitizedMethodCall;

        Parsed = true;
        return this;
    }

    [SwallowExceptions]
    public MethodInfo? ExtractMethod()
    {
        var parameters = ParseParameters(SanitizedParameters);
        return !TryParseDynamicMethod(SanitizedMethodCall, out var typeName, out var methodName)
            ? ParseNormalMethod(SanitizedMethodCall, parameters)
            : CachedMethods.GetCachedMethod(typeName, methodName, parameters) ??
              CachedMethods.GetCachedMethod(typeName, methodName, parameters[1..]);
    }

    public string[] SanitizeFrame()
    {
        var raw = Regex.Replace(stackFrame, @"\(wrapper[^\)]*\)\s", "").Replace(" at ", "");
        var parts = raw.Split('(', 2, StringSplitOptions.RemoveEmptyEntries);

        SanitizedMethodCall = parts[0].Trim();

        if (parts.Length < 2) {
            return parts;
        }

        var seg = parts[1].LastIndexOf(')');
        if (seg != -1) {
            parts[1] = parts[1][..seg];
        }

        SanitizedParameters = parts[1].Trim('(', ')', ' ');

        return parts;
    }

    [SwallowExceptions]
    private static Type[] ParseParameters(string parameters)
    {
        if (parameters.IsEmpty()) {
            return [];
        }

        List<Type> paramTypes = [];
        paramTypes.AddRange(TypeQualifier.SplitParameters(parameters)
            .Select(p => TypeQualifier.GlobalResolve(p.Trim()))
            .OfType<Type>());

        return paramTypes.ToArray();
    }

    public static bool TryParseDynamicMethod(string stackFrame, out string typeName, out string methodName)
    {
        typeName = methodName = "";

        var match = Regex.Match(stackFrame, "DMD<([^>]+)>");
        if (!match.Success) {
            return false;
        }

        var fullName = match.Groups[1].Value;
        var nameParts = fullName.Split("::", 2);
        if (nameParts.Length != 2) {
            return false;
        }

        typeName = nameParts[0];
        methodName = nameParts[1];
        return true;
    }

    public static MethodInfo? ParseNormalMethod(string partialMethodCall, Type[] parameters)
    {
        var lastDotIndex = partialMethodCall.LastIndexOf('.');
        if (lastDotIndex == -1) {
            return null;
        }

        var typeName = partialMethodCall[..lastDotIndex];
        var methodName = partialMethodCall[(lastDotIndex + 1)..];

        return CachedMethods.GetCachedMethod(typeName, methodName, parameters);
    }
}