using System.Linq;
using Cwl.LangMod;
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

        CwlMod.Warn<SourceHobby>($"failed to create hobby ID: {id}, replaced with Walking");
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.RerollHobby))]
    internal static void OnRerollInvalidHobby(Chara __instance)
    {
        var hobbies = EMono.sources.hobbies.alias;
        var filtered = __instance.source.hobbies
            .Where(hobbies.ContainsKey)
            .ToArray();

        if (filtered.Length != __instance.source.hobbies.Length) {
            foreach (var invalid in __instance.source.hobbies.Except(filtered)) {
                CwlMod.WarnWithPopup<SourceHobby>("cwl_warn_invalid_hobby".Loc("hobby".lang(), invalid, __instance.Name));
            }

            __instance.source.hobbies = filtered;
        }

        var works = __instance.source.works
            .Where(hobbies.ContainsKey)
            .ToArray();

        if (works.Length != __instance.source.works.Length) {
            foreach (var invalid in __instance.source.works.Except(works)) {
                CwlMod.WarnWithPopup<SourceHobby>("cwl_warn_invalid_hobby".Loc("work".lang(), invalid, __instance.Name));
            }

            __instance.source.works = works;
        }
    }
}