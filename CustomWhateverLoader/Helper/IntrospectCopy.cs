using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Cwl.Helper;

public static class IntrospectCopy
{
    public static void IntrospectCopyTo<T, TU>(this T source, TU target)
    {
        var access = AccessTools.all & ~BindingFlags.Static;
        var fields = typeof(T).GetFields(access);
        foreach (var dest in typeof(TU).GetFields(access)) {
            var field = fields.FirstOrDefault(f => f.Name == dest.Name &&
                                                   f.FieldType == dest.FieldType);
            if (field is null) {
                continue;
            }

            dest.SetValue(target, field.GetValue(source));
        }
    }
}