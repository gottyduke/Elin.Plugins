using HarmonyLib;

namespace Cwl.Patches.Charas;

[HarmonyPatch]
internal class CharaHumanSpeakPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.IsHumanSpeak), MethodType.Getter)]
    internal static void IsHumanSpeakTagged(Chara __instance, ref bool __result)
    {
        __result |= __instance.source.tag.Contains("humanSpeak");
    }
}