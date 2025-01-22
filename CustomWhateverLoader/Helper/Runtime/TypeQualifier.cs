using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using Cwl.LangMod;
using UnityEngine;

namespace Cwl.Helper.Runtime;

public class TypeQualifier
{
    internal static List<BaseUnityPlugin>? Plugins;
    internal static readonly List<TypeInfo> Declared = [];

    private static readonly Dictionary<string, Type> _qualifiedResults = [];

    public static Type? TryQualify<T>(params string[] unqualified) where T : EClass
    {
        foreach (var unq in unqualified) {
            if (unq.IsEmpty()) {
                continue;
            }

            if (_qualifiedResults.TryGetValue(unq, out var cached)) {
                return cached;
            }

            var types = Declared.OfDerived(typeof(T)).ToArray();
            var qualified = types.FirstOrDefault(t => t.FullName == unq) ??
                            types.FirstOrDefault(t => t.Name == unq);

            // check if data had case typo...
            if (qualified?.FullName is null) {
                qualified ??= types.FirstOrDefault(t => t.FullName!.Equals(unq, StringComparison.InvariantCultureIgnoreCase)) ??
                              types.FirstOrDefault(t => t.Name.Equals(unq, StringComparison.InvariantCultureIgnoreCase));
                if (qualified?.FullName is not null) {
                    CwlMod.Warn<TypeQualifier>($"typo in custom type {unq}, {qualified.FullName}");
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


    internal static void SafeQueryTypesOfAll()
    {
        Plugins ??= Resources.FindObjectsOfTypeAll<BaseUnityPlugin>().ToList();

        Declared.Clear();
        foreach (var plugin in Plugins.ToArray()) {
            try {
                Declared.AddRange(plugin.GetType().Assembly.DefinedTypes);
            } catch {
                CwlMod.Warn<TypeQualifier>("cwl_warn_decltype_missing".Loc(plugin.Info.Metadata.GUID));
                Plugins.Remove(plugin);
                // noexcept
            }
        }
    }
}