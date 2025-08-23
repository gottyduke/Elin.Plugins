using HarmonyLib;

namespace Cwl.Patches.Hobbies;

[HarmonyPatch]
internal class SafeGetHobbyPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Hobby), nameof(Hobby.source), MethodType.Getter)]
    internal static bool OnGetNullHobby(Hobby __instance, ref SourceHobby.Row __result)
    {
        var hobbies = EMono.sources.hobbies;
        var id = __instance.id;

        if (hobbies.map.ContainsKey(id)) {
            return true;
        }

        __result = hobbies.alias["Walk"];
        __instance.id = __result.id;

        CwlMod.Warn<SourceHobby>($"failed to create hobby ID: {id}, replacing with Walking");
        return false;
    }
}