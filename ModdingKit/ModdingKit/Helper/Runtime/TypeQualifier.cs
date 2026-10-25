using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        var packageAsm = ModUtil.FindFileProviderPackage(new(assembly.Location))?.title ??
                         Plugins.FirstOrDefault(p => p.GetType().Assembly == assembly)?.Info.Metadata.Name ??
                         baseAsm;

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
        Plugins = [
            ..ModManager.ListPluginObject
                .OfType<BaseUnityPlugin>(),
        ];

        foreach (var plugin in Plugins.ToArray()) {
            var types = QueryTypes(plugin.GetType().Assembly);
            if (types is null) {
                Plugins.Remove(plugin);
                continue;
            }

            Declared.UnionWith(types);
        }

        ClassCache.modTypes.UnionWith(Declared);
    }

    internal static List<TypeInfo>? QueryTypes(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        Type?[] candidates;
        try {
            candidates = assembly.GetTypes();
        } catch (ReflectionTypeLoadException ex) {
            candidates = ex.Types;
            var reasons = ex.LoaderExceptions
                .Select(e => e.Message)
                .Distinct()
                .Take(3);
            Debug.LogWarning($"#mod-content {name} has unresolvable types, skipping: {string.Join("; ", reasons)}");
        } catch (Exception ex) {
            Debug.LogWarning($"#mod-content failed to query types of {name}: {ex.Message}");
            return null;
            // noexcept
        }

        var declared = new List<TypeInfo>(candidates.Length);
        foreach (var type in candidates) {
            if (type is null) {
                continue;
            }

            try {
                var info = type.GetTypeInfo();
                if (info.IsDefined(typeof(CompilerGeneratedAttribute), false) || info.Name.Contains('<')) {
                    continue;
                }

                // test cast for missing dependency
                _ = typeof(object).IsAssignableFrom(info);
                declared.Add(info);
            } catch (Exception ex) {
                Debug.LogWarning($"#mod-content skipped type {type.FullName} in {name}: {ex.GetType().Name}");
                // noexcept
            }
        }

        return declared;
    }
}