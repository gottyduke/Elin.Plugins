﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Cwl.Helper.Runtime.Exceptions;

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


    private bool _parsed;
    public StackFrameType frameType = StackFrameType.Unknown;

    public MethodInfo? Method { get; private set; }
    public string StackFrame => stackFrame;

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
        // explicit dmd, discard mono jit
        if (raw.StartsWith("Rethrow as")) {
            frameType = StackFrameType.Rethrow;
        } else {
            Method = ExtractMethod(raw);
            frameType = Method is null ? StackFrameType.Unknown :
                raw.StartsWith("(wrapper dynamic-method)") ? StackFrameType.DynamicMethod : StackFrameType.Method;
        }

        _parsed = true;
        return this;
    }

    [SwallowExceptions]
    public static MethodInfo? ExtractMethod(string stackFrame)
    {
        var raw = Regex.Replace(stackFrame, @"^\(wrapper dynamic-method\)\s*", "");
        raw = Regex.Replace(raw, @"\s+\(at .+\)$", "");

        // Zone.RefreshPlaylist ()
        //Zone.DMD<Zone::CreatePlaylist>(Zone,System.Collections.Generic.List`1<int>&,Playlist)
        var parts = raw.Split('(', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) {
            return null;
        }

        var partialMethodCall = parts[0].Trim();
        var partialParameters = parts[1].Trim('(', ')', ' ');

        if (!TryParseDynamicMethod(partialMethodCall, out var typeName, out var methodName)) {
            return ParseNormalMethod(partialMethodCall, ParseParameters(partialParameters));
        }

        var pack = partialParameters.Split(',');
        partialParameters = string.Join(',', pack.Skip(1));
        return CachedMethods.GetCachedMethod(typeName, methodName, ParseParameters(partialParameters));
    }

    [SwallowExceptions]
    private static Type[] ParseParameters(string parameters)
    {
        List<Type> paramTypes = [];
        var paramStrings = parameters.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var paramStr in paramStrings) {
            var type = TypeQualifier.GlobalResolve(paramStr);
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