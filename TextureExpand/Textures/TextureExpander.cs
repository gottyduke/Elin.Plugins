using System.Collections.Generic;
using System.Linq;
using TextureExpand.Rules;

namespace TextureExpand.Textures;

internal static class TextureExpander
{
    internal static void Initialize()
    {
        EClass.sources.charas.Init();
        EClass.sources.elements.Init();

        RenderDataExpander.Begin();

        var groups = ReplaceSource.Collect();
        var latestImage = RuleRegistry.Build(groups);
        TextureCache.Enabled = TextureCache.Validate(latestImage);

        var expanded = new HashSet<string>();

        foreach (var tile in RuleRegistry.ByTile.Keys) {
            if (!RuleRegistry.RowsByTile.TryGetValue(tile, out var rows)) {
                continue;
            }

            foreach (var row in rows) {
                if (expanded.Contains(row.id)) {
                    continue;
                }

                if (RenderDataExpander.OnCharaSheet(row)) {
                    RenderDataExpander.Expand(row.id);
                    expanded.Add(row.id);
                }
            }
        }

        foreach (var charaId in RuleRegistry.ByCharaId.Keys) {
            if (expanded.Add(charaId)) {
                RenderDataExpander.Expand(charaId);
            }
        }

        foreach (var charaId in expanded) {
            RenderDataExpander.ReplaceTiles(charaId);
        }

        RenderDataExpander.Commit();

        foreach (var sheet in RenderDataExpander.CustomRenderDatas.Values.SelectMany(x => x)) {
            sheet.Texture!.Apply();
        }

        if (!TextureCache.Enabled) {
            TextureCache.Write();
        }

        RenderDataExpander.Clear();
        TileCompositor.Clear();
    }
}