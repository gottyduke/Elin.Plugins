using System.Collections.Generic;
using TextureExpand.Textures;

namespace TextureExpand.Rules;

internal static class RuleState
{
    internal static readonly HashSet<int> JudgeQueue = [];

    internal static readonly Dictionary<int, ReplaceRule> ReplaceRules = [];

    internal static readonly HashSet<int> TailingUids = [];

    internal static void Reset()
    {
        JudgeQueue.Clear();
        TailingUids.Clear();
        ReplaceRules.Clear();
    }

    internal static bool Judge(Chara chara)
    {
        List<ReplaceRule>? tileRules = null;
        if (RenderDataExpander.TileOrigins.TryGetValue(chara.source._idRenderData, out var origins) &&
            origins.TryGetValue(CurrentTile(chara), out var original)) {
            RuleRegistry.ByTile.TryGetValue(original, out tileRules);
        }

        RuleRegistry.ByCharaId.TryGetValue(chara.id, out var charaRules);

        if (tileRules is null && charaRules is null) {
            return ReplaceRules.Remove(chara.uid);
        }

        ReplaceRules.TryGetValue(chara.uid, out var before);
        ReplaceRules.Remove(chara.uid);

        if (tileRules is not null && charaRules is not null) {
            _ = Judge(chara, tileRules, true) || Judge(chara, charaRules, false);
        } else {
            Judge(chara, tileRules ?? charaRules!, false);
        }

        ReplaceRules.TryGetValue(chara.uid, out var after);
        return !ReferenceEquals(before, after);
    }

    private static int CurrentTile(Chara chara)
    {
        var row = chara.source;
        var tiles = row._tiles_snow.Length != 0 && EClass._zone.IsSnowCovered ? row._tiles_snow : row._tiles;
        if (tiles.Length == 0) {
            return -1;
        }

        var skin = tiles.Length > 1 && chara.idSkin == 0 && !row.staticSkin
            ? chara.uid % tiles.Length / 2 * 2 + (chara.IsMale ? 0 : 1)
            : chara.idSkin;
        return tiles[skin < tiles.Length ? skin : 0];
    }

    private static bool Judge(Chara chara, List<ReplaceRule> rules, bool conditionalOnly)
    {
        foreach (var rule in rules) {
            if (rule.ReplacedTile >= 0 && (!conditionalOnly || rule.HasJudges) && rule.IsValid(chara)) {
                ReplaceRules[chara.uid] = rule;
                return true;
            }
        }

        return false;
    }
}