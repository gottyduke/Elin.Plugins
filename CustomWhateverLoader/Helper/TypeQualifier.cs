using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace Cwl.Helper;

public class TypeQualifier
{
    private static List<TypeInfo>? _declared;

    public static Type? TryQualify(string unqualified, string assemblyOverride = "")
    {
        _declared ??= Resources.FindObjectsOfTypeAll<BaseUnityPlugin>()
            .SelectMany(p => p.GetType().Assembly.DefinedTypes)
            .Where(t => typeof(Act).IsAssignableFrom(t))
            .ToList();

        var qualified = _declared.FirstOrDefault(t => t.FullName == unqualified) ??
                        _declared.FirstOrDefault(t => t.Name == unqualified);
        if (qualified?.FullName is null ||
            (assemblyOverride is not "" && qualified.Assembly.GetName().Name != assemblyOverride)) {
            return null;
        }

        return qualified;
    }
}