using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using Cwl.LangMod;
using HarmonyLib;
using UnityEngine;
using Logger = HarmonyLib.Tools.Logger;

namespace Cwl.Helper;

public class TypeQualifier
{
    internal static List<BaseUnityPlugin>? Plugins;
    internal static readonly List<TypeInfo> Declared = [];

    private static readonly Dictionary<string, Type> _qualifiedResults = [];

    private static readonly Dictionary<string, Type> _aliasMapping = new() {
        ["byte"] = typeof(byte),
        ["sbyte"] = typeof(sbyte),
        ["short"] = typeof(short),
        ["ushort"] = typeof(ushort),
        ["int"] = typeof(int),
        ["uint"] = typeof(uint),
        ["long"] = typeof(long),
        ["ulong"] = typeof(ulong),
        ["float"] = typeof(float),
        ["single"] = typeof(float),
        ["double"] = typeof(double),
        ["decimal"] = typeof(decimal),
        ["object"] = typeof(object),
        ["bool"] = typeof(bool),
        ["char"] = typeof(char),
        ["string"] = typeof(string),
        ["void"] = typeof(void),
        ["nuint"] = typeof(nuint),
    };

    public static Type? TryQualify<T>(params string[] unqualified) where T : EClass
    {
        foreach (var unq in unqualified) {
            if (unq.IsEmpty()) {
                continue;
            }

            if (_qualifiedResults.TryGetValue(unq, out var cached)) {
                return cached;
            }

            Type? qualified = null;
            foreach (var t in Declared.OfDerived(typeof(T))) {
                if (t.FullName == unq || t.Name == unq) {
                    qualified = t;
                }

                if (qualified is null && (t.FullName?.Equals(unq, StringComparison.InvariantCultureIgnoreCase) is true ||
                                          t.Name.Equals(unq, StringComparison.InvariantCultureIgnoreCase))) {
                    qualified = t;
                    CwlMod.WarnWithPopup<TypeQualifier>("cwl_warn_qualify_typo".Loc(unq, qualified.FullName));
                }

                if (qualified is not null) {
                    break;
                }
            }

            if (qualified?.FullName is null) {
                continue;
            }

            return _qualifiedResults[unq] = qualified;
        }

        return null;
    }

    public static Type? GlobalResolve(string unresolvedStr)
    {
        if (unresolvedStr.StartsWith("at <")) {
            return null;
        }

        var trimmedParam = unresolvedStr.Trim();
        if (trimmedParam.IsEmpty()) {
            return null;
        }

        if (_qualifiedResults.TryGetValue(trimmedParam, out var paramType)) {
            return paramType;
        }

        if (!trimmedParam.EndsWith('>')) {
            trimmedParam = trimmedParam.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        }

        var typeName = ProcessStandardGenerics(trimmedParam);
        if (_aliasMapping.TryGetValue(typeName, out var type)) {
            typeName = typeName.Replace(typeName, type.FullName);
        }

        // DMD
        var hasGeneric = Regex.Match(typeName, "<([^>]+)>");
        List<Type> generics = [];
        if (hasGeneric.Success) {
            foreach (var alias in hasGeneric.Groups[1].Value.Split(',')) {
                if (GlobalResolve(alias) is { } aliasType) {
                    generics.Add(aliasType);
                }
            }

            typeName = typeName.Replace(hasGeneric.Groups[0].Value, "");
        }

        // DMD NestType
        typeName = typeName.Replace('/', '+');

        // Ref
        var refParam = typeName.EndsWith('&');
        if (refParam) {
            typeName = typeName[..^1];
        }

        paramType = TryGetType(typeName);
        if (paramType == null) {
            return null;
        }

        // Generic
        if (generics.Count > 0) {
            paramType = paramType.MakeGenericType(generics.ToArray());
        }

        if (refParam) {
            paramType = paramType.MakeByRefType();
        }

        return _qualifiedResults[trimmedParam] = paramType;
    }

    private static string ProcessStandardGenerics(string typeName)
    {
        var match = Regex.Match(typeName, @"^(?<main>.*?)(?<!`)`(?<arity>\d+)\[(?<args>.*)\]$");
        if (!match.Success) {
            return typeName;
        }

        var main = match.Groups["main"].Value;
        var arity = match.Groups["arity"].Value;
        var args = match.Groups["args"].Value;

        List<string> processedArgs = [];
        foreach (var arg in SplitGenericArgs(args)) {
            var processedArg = arg.Trim();
            var resolvedType = TryGetType(processedArg) ?? TryGetType("System." + processedArg);

            processedArgs.Add(resolvedType?.FullName ?? processedArg);
        }

        return $"{main}`{arity}[{string.Join(",", processedArgs.Select(a => $"[{a}]"))}]";
    }

    public static IEnumerable<string> SplitGenericArgs(string args)
    {
        var depth = 0;
        var lastSplit = 0;
        for (var i = 0; i < args.Length; ++i) {
            switch (args[i]) {
                case '[':
                    depth++;
                    break;
                case ']':
                    depth--;
                    break;
                case ',' when depth == 0:
                    yield return args[lastSplit..(i - lastSplit)];
                    lastSplit = i + 1;
                    break;
            }
        }

        yield return args[lastSplit..];
    }

    public static List<string> SplitParameters(string input)
    {
        var segments = new List<string>();
        var current = new StringBuilder();
        var depth = 0;

        foreach (var c in input) {
            switch (c) {
                case '<':
                    depth++;
                    current.Append(c);
                    break;
                case '>':
                    depth--;
                    current.Append(c);
                    break;
                case ',' when depth == 0:
                    segments.Add(current.ToString());
                    current.Clear();
                    break;
                default:
                    current.Append(c);
                    break;
            }
        }

        if (current.Length > 0) {
            segments.Add(current.ToString());
        }

        return segments;
    }

    [SwallowExceptions]
    internal static void SafeQueryTypesOfAll()
    {
        Plugins ??= Resources.FindObjectsOfTypeAll<BaseUnityPlugin>().ToList();
        Declared.Clear();

        foreach (var plugin in Plugins.ToArray()) {
            try {
                var types = plugin.GetType().Assembly.DefinedTypes.ToArray();
                // test cast for missing dependency
                _ = types.Select(ti => typeof(object).IsAssignableFrom(ti)).ToArray();
                Declared.AddRange(types);
            } catch {
                CwlMod.Log<TypeQualifier>("cwl_warn_decltype_missing".Loc(plugin.Info.Metadata.GUID));
                Plugins.Remove(plugin);
                // noexcept
            }
        }
    }

    internal static Type? TryGetType(string typeName)
    {
        try {
            Logger.ChannelFilter = Logger.LogChannel.Error;
            return AccessTools.TypeByName(typeName);
        } catch {
            return null;
            // noexcept
        }
    }
}