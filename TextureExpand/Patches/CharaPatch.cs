using System.Collections.Generic;
using HarmonyLib;
using TextureExpand.Rules;
using TextureExpand.Textures;
using UnityEngine;

namespace TextureExpand.Patches;

[HarmonyPatch]
internal class CharaPatch
{
    private static readonly Dictionary<(int, int), Sprite> _sprites = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Chara), nameof(Chara._Move), typeof(Point), typeof(Card.MoveType))]
    internal static void OnMove(Chara __instance, Card.MoveResult __result)
    {
        if (__result == Card.MoveResult.Success && RuleKeywords.Has(__instance.id, Trigger.Move) &&
            __instance.pos?.cell != null) {
            RuleState.JudgeQueue.Add(__instance.uid);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.hostility), MethodType.Setter)]
    internal static void OnSetHostility(Chara __instance)
    {
        if (RuleKeywords.Has(__instance.id, Trigger.Hostility)) {
            RuleState.JudgeQueue.Add(__instance.uid);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.AddCondition), typeof(Condition), typeof(bool))]
    internal static void OnAddCondition(Chara __instance)
    {
        if (RuleKeywords.Has(__instance.id, Trigger.Condition)) {
            RuleState.JudgeQueue.Add(__instance.uid);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), nameof(Chara.GetSprite), typeof(int))]
    internal static bool OnGetSprite(Chara __instance, ref Sprite __result)
    {
        if (!RuleState.ReplaceRules.TryGetValue(__instance.uid, out var rule)) {
            return true;
        }

        var renderData = __instance.source.renderData;
        var pass = renderData.pass;
        if (pass == null || pass.mat.GetTexture(RenderDataExpander.MainTex) is not Texture2D sheet) {
            return true;
        }

        var key = (sheet.GetInstanceID(), rule.ReplacedTile);
        if (!_sprites.TryGetValue(key, out var sprite) || !sprite) {
            var pmesh = pass.pmesh;
            var cell = new Vector2Int(Mathf.RoundToInt(pmesh.size.x * 100f), Mathf.RoundToInt(pmesh.size.y * 100f));
            var columns = (int)pmesh.tiling.x;

            var x = rule.ReplacedTile % columns * cell.x;
            var y = sheet.height - (rule.ReplacedTile / columns + 1) * cell.y;
            var height = cell.y * (renderData.multiSize ? 2 : 1);

            sprite = Sprite.Create(sheet, new(x, y, cell.x, height), Vector2.zero, 100f, 0u, SpriteMeshType.FullRect);
            sprite.name = rule.Key;
            _sprites[key] = sprite;
        }

        __result = sprite;
        return false;
    }
}