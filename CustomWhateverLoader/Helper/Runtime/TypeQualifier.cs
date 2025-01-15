using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using Cwl.LangMod;
using HarmonyLib;
using UnityEngine;

namespace Cwl.Helper.Runtime;

public class TypeQualifier
{
    internal static readonly Dictionary<TypeInfo, Type> Declared = [];
    internal static List<BaseUnityPlugin>? Plugins;
    private static readonly Dictionary<string, Type> _cached = [];

    // a reverse lookup of base: derived[]
    public static ILookup<Type, TypeInfo> TypeLookup => Declared.ToLookup(kv => kv.Value, kv => kv.Key);

    public static Type? TryQualify<T>(params string[] unqualified) where T : EClass
    {
        foreach (var unq in unqualified) {
            if (unq.IsEmpty()) {
                continue;
            }

            if (_cached.TryGetValue(unq, out var cached)) {
                return cached;
            }

            var types = Declared.Keys.OfDerived(typeof(T)).ToArray();
            var qualified = types.FirstOrDefault(t => t.FullName == unq) ??
                            types.FirstOrDefault(t => t.Name == unq);

            // check if data had case typo...
            if (qualified?.FullName is null) {
                qualified ??= types.FirstOrDefault(t => t.FullName!.Equals(unq,
                                  StringComparison.InvariantCultureIgnoreCase)) ??
                              types.FirstOrDefault(t => t.Name.Equals(unq,
                                  StringComparison.InvariantCultureIgnoreCase));
                if (qualified?.FullName is not null) {
                    CwlMod.Warn<TypeQualifier>($"typo in custom type {unq}, {qualified.FullName}");
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
    internal static void SafeQueryTypes<T>(Type? fallback = null) where T : notnull
    {
        Plugins ??= Resources.FindObjectsOfTypeAll<BaseUnityPlugin>().ToList();

        foreach (var plugin in Plugins.ToArray()) {
            try {
                plugin.GetType().Assembly.DefinedTypes
                    .OfDerived(typeof(T))
                    .Do(t => Declared.TryAdd(t, typeof(T)));
            } catch {
                CwlMod.Warn<TypeQualifier>("cwl_warn_decltype_missing".Loc(plugin.Info.Metadata.GUID));
                Plugins.Remove(plugin);
                // noexcept
            }
        }
    }
}