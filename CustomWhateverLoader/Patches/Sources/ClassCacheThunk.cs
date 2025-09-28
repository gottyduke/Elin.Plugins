using System.Reflection;
using HarmonyLib;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class ClassCacheThunk
{
    internal static bool Prepare()
    {
        return CwlConfig.CacheTypes;
    }

    internal static MethodInfo TargetMethod()
    {
        return AccessTools.Method(
                typeof(ClassCache<object>),
                nameof(ClassCache<>.Create),
                [typeof(string), typeof(string)])
            .MakeGenericMethod(typeof(object));
    }

    [HarmonyPrefix]
    internal static bool CreateThunk(ref object __result, string id)
    {
        if (!ClassCache.caches.dict.TryGetValue(id, out var func) ||
            func() is not { } instance) {
            return true;
        }

        __result = instance;
        return false;
    }
}