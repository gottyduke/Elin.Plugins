using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using Cwl.LangMod;
using Cwl.Loader;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Helper;

public class TypeQualifier
{
    private static readonly HashSet<TypeInfo> _declared = [];
    private static readonly Dictionary<string, Type> _cached = [];
    private static List<BaseUnityPlugin>? _plugins;

    public static Type? TryQualify<T>(string unqualified, string assemblyOverride = "") where T : EClass
    {
        if (unqualified is null or "") {
            return null;
        }

        if (_cached.TryGetValue(unqualified, out var cached)) {
            return cached;
        }

        var types = _declared.Where(t => typeof(T).IsAssignableFrom(t)).ToArray();
        var qualified = types.FirstOrDefault(t => t.FullName == unqualified) ??
                        types.FirstOrDefault(t => t.Name == unqualified) ?? 
                        types.FirstOrDefault(t => t.FullName!.Equals(unqualified, StringComparison.InvariantCultureIgnoreCase)) ?? 
                        types.FirstOrDefault(t => t.Name.Equals(unqualified, StringComparison.InvariantCultureIgnoreCase));

        if (qualified?.FullName is null ||
            (assemblyOverride is not "" && qualified.Assembly.GetName().Name != assemblyOverride)) {
            return null;
        }

        _cached[unqualified] = qualified;
        return qualified;
    }

    // cannot use linq to query due to some users might install mod without dependency...sigh
    internal static void SafeQueryTypes<T>() where T : EClass
    {
        _plugins ??= Resources.FindObjectsOfTypeAll<BaseUnityPlugin>().ToList();
        
        List<TypeInfo> declared = [];
        foreach (var plugin in _plugins.ToList()) {
            try {
                var types = plugin.GetType().Assembly.DefinedTypes
                    .Where(t => typeof(T).IsAssignableFrom(t));
                declared.AddRange(types);
            } catch {
                CwlMod.Warn("cwl_warn_decltype_missing".Loc(plugin.Info.Metadata.GUID));
                _plugins.Remove(plugin);
                // noexcept
            }
        }

        declared.Do(decl => _declared.Add(decl));
    }
}