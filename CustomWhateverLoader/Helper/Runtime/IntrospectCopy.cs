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

        var srcType = source!.GetType();
        if (!_cached.TryGetValue(srcType, out var srcFields)) {
            _cached[srcType] = srcFields = srcType.GetFields(access);
        }

        var dstType = target!.GetType();
        if (!_cached.TryGetValue(dstType, out var dstFields)) {
            _cached[dstType] = dstFields = dstType.GetFields(access);
        }

        foreach (var dest in dstFields) {
            var field = srcFields.FirstOrDefault(f => f.Name == dest.Name &&
                                                      f.FieldType == dest.FieldType);
            if (field is null) {
                continue;
            }

            dest.SetValue(target, field.GetValue(source));
        }
    }

    public static T GetIntrospectCopy<T>(this T source) where T : notnull, new()
    {
        T val = new();
        source.IntrospectCopyTo(val);
        return val;
    }
}