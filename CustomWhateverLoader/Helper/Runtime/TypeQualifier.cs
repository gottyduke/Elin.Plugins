using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using BepInEx;
using Cwl.Helper.Extensions;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Helper;

public class TypeQualifier
{
    private static readonly Dictionary<string, Type> _qualifiedResults = [];

    internal static readonly List<TypeInfo> Declared = [];

    internal static readonly Dictionary<string, Type> AliasMapping = new() {
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

    [field: AllowNull]
    public static Dictionary<Assembly, BaseUnityPlugin> Plugins =>
        field ??= ModManager.ListPluginObject
            .OfType<BaseUnityPlugin>()
            .ToDictionary(p => p.GetType().Assembly);

    public static string GetMappedAssemblyName(Assembly assembly)
    {
        return Plugins.TryGetValue(assembly, out var plugin)
            ? plugin.Info.Metadata.Name
            : assembly.GetName().Name;
    }

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
        var sanitized = unresolvedStr.Replace('/', '+').Trim();
        if (sanitized.IsEmpty()) {
            return null;
        }

        if (_qualifiedResults.TryGetValue(sanitized, out var paramType)) {
            return paramType;
        }

        paramType = Type.GetType(sanitized);
        if (paramType is null) {
            foreach (var asm in AccessTools.AllAssemblies()) {
                paramType = Type.GetType($"{sanitized}, {asm.FullName}");
                if (paramType is not null) {
                    break;
                }
            }
        }

        return paramType is not null ? _qualifiedResults[sanitized] = paramType : null;
    }

    [SwallowExceptions]
    internal static void SafeQueryTypesOfAll()
    {
        Declared.Clear();

        foreach (var (asm, plugin) in Plugins.ToArray()) {
            try {
                var types = asm.DefinedTypes.ToArray();
                // test cast for missing dependency
                _ = types.Select(ti => typeof(object).IsAssignableFrom(ti)).ToArray();
                Declared.AddRange(types);
            } catch {
                CwlMod.Log<TypeQualifier>("cwl_warn_decltype_missing".Loc(plugin.Info.Metadata.GUID));
                Plugins.Remove(asm);
                // noexcept
            }
        }
    }
}