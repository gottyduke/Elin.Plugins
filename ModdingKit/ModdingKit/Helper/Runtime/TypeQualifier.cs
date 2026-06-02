using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace EModding.Helper.Runtime;

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

    public static readonly HashSet<TypeInfo> Declared = [];

    public static readonly Dictionary<Assembly, string> MappedAssemblyNames = [];

    public static List<BaseUnityPlugin> Plugins { get; private set; } = [];

    public static string GetMappedAssemblyName(Assembly assembly)
    {
        if (MappedAssemblyNames.TryGetValue(assembly, out var result)) {
            return result;
        }

        if (assembly.IsRoslynScript) {
            return "ℛ*";
        }

        var baseAsm = assembly.GetName().Name.Replace(" ", "");
        if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location)) {
            return baseAsm;
        }

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

    public static Type? TryQualify(string unqualified)
    {
        if (string.IsNullOrEmpty(unqualified)) {
            return null;
        }

        if (_qualifiedResults.TryGetValue(unqualified, out var cached)) {
            return cached;
        }

        Type? qualified = null;
        foreach (var t in Declared) {
            if (t.FullName == unqualified || t.Name == unqualified) {
                qualified = t;
            }

            if (qualified is null && (t.FullName?.Equals(unqualified, StringComparison.InvariantCultureIgnoreCase) is true ||
                                      t.Name.Equals(unqualified, StringComparison.InvariantCultureIgnoreCase))) {
                qualified = t;
            }

            if (qualified is not null) {
                break;
            }
        }

        if (qualified?.FullName is null) {
            return null;
        }

        Debug.Log($"#mod-content qualified type: {unqualified} -> {qualified}");
        return _qualifiedResults[unqualified] = qualified;
    }

    public static Type? GlobalResolve(string unresolvedStr)
    {
        var sanitized = unresolvedStr.Replace('/', '+').Trim();
        if (string.IsNullOrEmpty(sanitized)) {
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

    internal static void SafeQueryTypesOfAll()
    {
        Plugins = ModManager.ListPluginObject
            .OfType<BaseUnityPlugin>()
            .ToList();

        foreach (var plugin in Plugins.ToArray()) {
            try {
                var types = plugin.GetType().Assembly.DefinedTypes.ToArray();
                // test cast for missing dependency
                _ = types.Select(ti => typeof(object).IsAssignableFrom(ti)).ToArray();
                Declared.UnionWith(types);
            } catch {
                Plugins.Remove(plugin);
                // noexcept
            }
        }
    }
}