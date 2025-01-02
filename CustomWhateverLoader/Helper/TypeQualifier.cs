using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using Cwl.LangMod;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Helper;

public class TypeQualifier
{
    internal static readonly HashSet<TypeInfo> Declared = [];
    internal static List<BaseUnityPlugin>? Plugins;
    private static readonly Dictionary<string, Type> _cached = [];

    public static Type? TryQualify<T>(params string[] unqualified) where T : EClass
    {
        foreach (var unq in unqualified) {
            if (unq is null or "") {
                continue;
            }

            if (_cached.TryGetValue(unq, out var cached)) {
                return cached;
            }

            var types = Declared.Where(t => typeof(T).IsAssignableFrom(t)).ToArray();
            var qualified = types.FirstOrDefault(t => t.FullName == unq) ??
                            types.FirstOrDefault(t => t.Name == unq);

            // check if data had case typo...
            if (qualified?.FullName is null) {
                qualified ??= types.FirstOrDefault(t => t.FullName!.Equals(unq,
                                  StringComparison.InvariantCultureIgnoreCase)) ??
                              types.FirstOrDefault(t => t.Name.Equals(unq,
                                  StringComparison.InvariantCultureIgnoreCase));
                if (qualified?.FullName is not null) {
                    CwlMod.Warn($"typo in custom type {unq}, {qualified.FullName}");
                }
            }

            if (qualified?.FullName is null) {
                continue;
            }

            _cached[unq] = qualified;
            return qualified;
        }

        return null;
    }

    // cannot use linq to query due to some users might install mod without dependency...sigh
    internal static void SafeQueryTypes<T>() where T : EClass
    {
        Plugins ??= Resources.FindObjectsOfTypeAll<BaseUnityPlugin>().ToList();

        List<TypeInfo> declared = [];
        foreach (var plugin in Plugins.ToList()) {
            try {
                var types = plugin.GetType().Assembly.DefinedTypes
                    .Where(t => typeof(T).IsAssignableFrom(t));
                declared.AddRange(types);
            } catch {
                CwlMod.Warn("cwl_warn_decltype_missing".Loc(plugin.Info.Metadata.GUID));
                Plugins.Remove(plugin);
                // noexcept
            }
        }

        declared.Do(decl => Declared.Add(decl));
    }
}