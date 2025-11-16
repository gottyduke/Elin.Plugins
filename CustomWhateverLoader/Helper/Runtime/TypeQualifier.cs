using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Helper;

public class TypeQualifier
{
    private static readonly Dictionary<string, Type> _qualifiedResults = [];

    public static readonly Dictionary<string, Type> AliasMapping = new() {
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

    public static readonly List<TypeInfo> Declared = [];

    public static readonly Dictionary<Assembly, string> MappedAssemblyNames = [];

    [field: AllowNull]
    public static List<BaseUnityPlugin> Plugins =>
        field ??= ModManager.ListPluginObject
            .OfType<BaseUnityPlugin>()
            .ToList();

    public static string GetMappedAssemblyName(Assembly assembly)
    {
        if (MappedAssemblyNames.TryGetValue(assembly, out var result)) {
            return result;
        }

        var baseAsm = assembly.GetName().Name.Replace(" ", "");
        var baseDir = Path.GetDirectoryName(assembly.Location)!.NormalizePath();

        var packageAsm = BaseModManager.Instance.packages
                             .FirstOrDefault(p => p.dirInfo.FullName.NormalizePath() == baseDir)?.title
                         ?? Plugins.FirstOrDefault(p => p.GetType().Assembly == assembly)?.Info.Metadata.Name
                         ?? baseAsm;

        packageAsm = packageAsm.Replace(" ", "").Replace(baseAsm, "");
        packageAsm = string.IsNullOrWhiteSpace(packageAsm)
            ? baseAsm
            : StringHelper.MergeOverlap(packageAsm, baseAsm);

        return MappedAssemblyNames[assembly] = packageAsm;
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