using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Cwl.Helper;

internal static class IntrospectCopy
{
    internal static void IntrospectCopyTo<T, TU>(this T source, TU dest)
    {
        var flag = AccessTools.all & ~BindingFlags.Static;
        var fields = typeof(T).GetFields(flag);
        foreach (var d in typeof(TU).GetFields(flag)) {
            var field = fields.FirstOrDefault(f => f.Name == d.Name && f.FieldType == d.FieldType);
            if (field is null) {
                continue;
            }

            d.SetValue(dest, field.GetValue(source));
        }
    }
}