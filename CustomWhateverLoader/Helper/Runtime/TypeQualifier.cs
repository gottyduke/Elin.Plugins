using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using Cwl.LangMod;
using UnityEngine;

namespace Cwl.Helper.Runtime;

public class TypeQualifier
{
    internal static List<BaseUnityPlugin>? Plugins;
    internal static readonly List<TypeInfo> Declared = [];

    private static readonly Dictionary<string, Type> _qualifiedResults = [];

    private static readonly Dictionary<string, Type> _aliasMapping = new() {
        { "byte", typeof(byte) },
        { "sbyte", typeof(sbyte) },
        { "short", typeof(short) },
        { "ushort", typeof(ushort) },
        { "int", typeof(int) },
        { "uint", typeof(uint) },
        { "long", typeof(long) },
        { "ulong", typeof(ulong) },
        { "float", typeof(float) },
        { "double", typeof(double) },
        { "decimal", typeof(decimal) },
        { "object", typeof(object) },
        { "bool", typeof(bool) },
        { "char", typeof(char) },
        { "string", typeof(string) },
        { "void", typeof(void) },
        { "nuint", typeof(nuint) },
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
                    CwlMod.WarnWithPopup<TypeQualifier>($"typo in custom type {unq}, {qualified.FullName}");
                }

                if (qualified is not null) {
                    break;
                }
            }

            if (qualified?.FullName is null) {
                continue;
            }

            _qualifiedResults[unq] = qualified;
            return qualified;
        }

        return null;
    }

    public static Type? GlobalResolve(string unresolvedStr)
    {
        var trimmedParam = unresolvedStr.Trim();
        if (string.IsNullOrEmpty(trimmedParam)) {
            return null;
        }

        var paramParts = trimmedParam.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (paramParts.Length == 0) {
            return null;
        }

        // DMD
        var typeName = paramParts[0];
        var dmdGeneric = Regex.Match(typeName, "<([^>]+)>");
        if (dmdGeneric.Success) {
            foreach (var alias in dmdGeneric.Groups[1].Value.Split(',')) {
                var trimmedAlias = alias.Trim();
                if (_aliasMapping.TryGetValue(trimmedAlias, out var type)) {
                    typeName = typeName.Replace(trimmedAlias, type.FullName);
                }
            }

            typeName = typeName.Replace('<', '[').Replace('>', ']');
        }

        // DMD NestType
        typeName = typeName.Replace('/', '+');

        // Ref
        var refParam = typeName.EndsWith('&');
        if (refParam) {
            typeName = typeName[..^1];
        }

        var paramType = CachedMethods.TryGetType(typeName);
        if (paramType == null) {
            return null;
        }

        if (refParam) {
            paramType = paramType.MakeByRefType();
        }

        return paramType;
    }

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
}