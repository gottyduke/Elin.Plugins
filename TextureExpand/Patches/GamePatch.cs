using HarmonyLib;
using TextureExpand.Rules;

namespace TextureExpand.Patches;

[HarmonyPatch]
internal class GamePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game), nameof(Game.OnBeforeInstantiate))]
    internal static void OnBeforeInstantiate()
    {
        RuleState.Reset();
    }
}