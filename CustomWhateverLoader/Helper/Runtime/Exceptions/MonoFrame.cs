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
    ];

    private bool _parsed;
    public StackFrameType frameType = StackFrameType.Unknown;

    public MethodInfo? Method { get; private set; }
    public string StackFrame => stackFrame;
    public string SanitizedMethodCall { get; private set; } = "";
    public string SanitizedParameters { get; private set; } = "";
    public string DetailedMethodCall { get; private set; } = "";

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

    [SwallowExceptions]
    public MonoFrame Parse()
    {
        if (_parsed) {
            return this;
        }

        var raw = stackFrame.Trim();
        SanitizeFrame();

        // explicit dmd, discard mono jit
        if (raw.StartsWith("Rethrow as")) {
            frameType = StackFrameType.Rethrow;
        } else {
            Method = ExtractMethod();
            frameType = Method is null ? StackFrameType.Unknown :
                raw.StartsWith("(wrapper dynamic-method)") ? StackFrameType.DynamicMethod : StackFrameType.Method;
        }

        DetailedMethodCall = frameType switch {
            StackFrameType.Method or StackFrameType.DynamicMethod
                when Method is { DeclaringType: not null } mi
                => _vendorExclusion.Any(mi.DeclaringType.Assembly.ManifestModule.Name.StartsWith)
                    ? mi.GetDetail(false)
                    : mi.GetAssemblyDetailColor(false),
            _ => SanitizedMethodCall,
        };

        _parsed = true;
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
        foreach (var paramStr in TypeQualifier.SplitParameters(parameters)) {
            var type = TypeQualifier.GlobalResolve(paramStr.Trim());
            if (type is not null) {
                paramTypes.Add(type);
            }
        }

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