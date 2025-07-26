using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Cwl.Helper;

public static class IntrospectCopy
{
    public static void IntrospectCopyTo<T, TU>(this T source, TU target)
    {
        var srcType = source!.GetType();
        var srcFields = srcType.GetCachedFields();
        var dstType = target!.GetType();

        foreach (var dest in dstType.GetCachedFields()) {
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