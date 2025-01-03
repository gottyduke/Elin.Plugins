using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Cwl.Helper;

public static class IntrospectCopy
{
    private static readonly Dictionary<Type, FieldInfo[]> _cached = [];

    public static void IntrospectCopyTo<T, TU>(this T source, TU target, BindingFlags? flags = null)
    {
        var access = flags ?? AccessTools.all & ~BindingFlags.Static;

        if (!_cached.TryGetValue(typeof(T), out var srcFields)) {
            _cached[typeof(T)] = srcFields = typeof(T).GetFields(access);
        }

        if (!_cached.TryGetValue(typeof(TU), out var destFields)) {
            _cached[typeof(TU)] = destFields = typeof(TU).GetFields(access);
        }

        foreach (var dest in destFields) {
            var field = srcFields.FirstOrDefault(f => f.Name == dest.Name &&
                                                      f.FieldType == dest.FieldType);
            if (field is null) {
                continue;
            }

            dest.SetValue(target, field.GetValue(source));
        }
    }
}