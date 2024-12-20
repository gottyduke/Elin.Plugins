using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using Cwl.LangMod;
using Cwl.Loader;
using UnityEngine;

namespace Cwl.Helper;

public class TypeQualifier
{
    private static List<TypeInfo>? _declared;
    
    public static Type? TryQualify(string unqualified, string assemblyOverride = "")
    {
        if (_declared is null) {
            SafeQueryTypes();
        }
        
        var qualified = _declared.FirstOrDefault(t => t.FullName == unqualified) ??
                        _declared.FirstOrDefault(t => t.Name == unqualified);
        if (qualified?.FullName is null ||
            (assemblyOverride is not "" && qualified.Assembly.GetName().Name != assemblyOverride)) {
            return null;
        }

        return qualified;
    }

    // cannot use linq to query due to some users might install mod without dependency...sigh
    internal static void SafeQueryTypes()
    {
        List<TypeInfo> declared = [];
        foreach (var plugin in Resources.FindObjectsOfTypeAll<BaseUnityPlugin>()) {
            try {
                var types = plugin.GetType().Assembly.DefinedTypes
                    .Where(t => typeof(Act).IsAssignableFrom(t));
                declared.AddRange(types);
            } catch (Exception ex) {
                CwlMod.Warn("cwl_warn_decltype_missing".Loc(plugin.Info.Metadata.GUID, ex.Message));
                // noexcept
            }
        }
        _declared = declared;
    }
}