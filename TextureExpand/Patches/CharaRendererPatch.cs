using HarmonyLib;
using TextureExpand.Helper;
using TextureExpand.Rules;
using UnityEngine;

namespace TextureExpand.Patches;

[HarmonyPatch]
internal class CharaRendererPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CharaRenderer), nameof(CharaRenderer.Draw),
        [typeof(RenderParam), typeof(Vector3), typeof(bool)],
        [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal])]
    internal static void OnDraw(CharaRenderer __instance, RenderParam p)
    {
        if (!RuleRegistry.HasRules) {
            return;
        }

        var owner = __instance.owner;
        if (RuleState.JudgeQueue.Remove(owner.uid) && RuleState.Judge(owner, (int)Mathf.Abs(p.tile))) {
            ActorSprite.Reload(__instance);
        }

        if (owner.renderer?.replacer != null || Player.seedHallucination != 0) {
            return;
        }

        if (!RuleState.ReplaceRules.TryGetValue(owner.uid, out var rule)) {
            return;
        }

        p.tile = rule.ReplacedTile * (owner.flipX ? -1 : 1);
    }
}