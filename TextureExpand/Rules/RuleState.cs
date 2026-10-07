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

    internal static bool Judge(Chara chara, int tile)
    {
        List<ReplaceRule>? tileRules = null;
        if (RenderDataExpander.TileOrigins.TryGetValue(chara.source._idRenderData, out var origins) &&
            origins.TryGetValue(tile, out var original)) {
            RuleRegistry.ByTile.TryGetValue(original, out tileRules);
        }

        RuleRegistry.ByCharaId.TryGetValue(chara.id, out var charaRules);

        if (tileRules is null && charaRules is null) {
            return false;
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