using System;
using System.Collections.Generic;
using System.Linq;
using TextureExpand.Rules;
using UnityEngine;

namespace TextureExpand.Textures;

internal static class RenderDataExpander
{
    private const string RenderDataPath = "Scene/Render/Data/";
    private const string ExpandSuffix = "_expand_";

    private const string CharaSheet = "objs_C";

    internal static readonly int MainTex = Shader.PropertyToID("_MainTex");

    internal static readonly Dictionary<string, List<CustomRenderData>> CustomRenderDatas = [];

    internal static readonly Dictionary<string, Dictionary<int, int>> TileOrigins = [];

    private static readonly Dictionary<string, RenderData?> _sources = [];
    private static readonly Dictionary<string, Texture2D?> _sourceTextures = [];
    private static readonly Dictionary<string, bool> _onCharaSheet = [];
    private static readonly Dictionary<string, (string Name, string? Option, RenderData Instance)> _bases = [];

    private static readonly Dictionary<string, int> _slots = [];
    private static readonly HashSet<int> _usedSlots = [];

    private static readonly List<MeshPass> _passes = [];

    internal static void Begin()
    {
        _passes.AddRange(EClass.scene.passes);
    }

    internal static void Commit()
    {
        EClass.scene.passes = [.._passes];
    }

    internal static void Clear()
    {
        CustomRenderDatas.Clear();
        _sources.Clear();
        _sourceTextures.Clear();
        _onCharaSheet.Clear();
        _bases.Clear();
        _slots.Clear();
        _usedSlots.Clear();
        _passes.Clear();
    }

    internal static bool HasSourceTexture(SourceChara.Row row)
    {
        if (_sourceTextures.TryGetValue(row._idRenderData, out var texture)) {
            return texture is not null;
        }

        var id = row._idRenderData;
        if (id.StartsWith("@")) {
            id = id[1..];
        }

        var data = LoadSource(id);
        texture = data?.pass?.pmesh != null
            ? data.pass.mat?.GetTexture(MainTex) as Texture2D
            : null;

        _sourceTextures[row._idRenderData] = texture;
        return texture is not null;
    }

    internal static bool OnCharaSheet(SourceChara.Row row)
    {
        if (_onCharaSheet.TryGetValue(row.idRenderData, out var result)) {
            return result;
        }

        var texture = LoadSource(row.idRenderData)?.pass?.mat?.GetTexture(MainTex);
        return _onCharaSheet[row.idRenderData] = texture?.name == CharaSheet;
    }

    internal static void Expand(string charaId)
    {
        if (!EClass.sources.charas.map.TryGetValue(charaId, out var row)) {
            TextureExpand.LogError("Not found Chara: " + charaId);
            return;
        }

        var (name, option, renderData) = GetBase(row);
        var pass = renderData.pass;
        if (pass == null) {
            TextureExpand.LogError("Not found Pass: " + row.pathRenderData + name);
            return;
        }

        if (pass.mat.GetTexture(MainTex) is not Texture2D source) {
            TextureExpand.LogError("Not found texture: " + row.pathRenderData + name);
            return;
        }

        var size = pass.pmesh.size;
        var cell = new Vector2Int(Mathf.RoundToInt(size.x * 100f), Mathf.RoundToInt(size.y * 100f));

        if (!CustomRenderDatas.TryGetValue(name, out var sheets)) {
            CustomRenderDatas[name] = sheets = [];
        }

        var rules = new List<ReplaceRule>();
        foreach (var tile in row.tiles.Concat(row.tiles_snow)) {
            if (RuleRegistry.ByTile.TryGetValue(tile, out var tileRules)) {
                rules.AddRange(tileRules);
            }
        }

        if (RuleRegistry.ByCharaId.TryGetValue(charaId, out var charaRules)) {
            rules.AddRange(charaRules);
        }

        var needed = row.tiles.Concat(row.tiles_snow).Distinct().Count() + rules.Count;
        var sheet = sheets.Find(s => HasRoom(s, cell, needed)) ??
                    CreateSheet(row, name, option, renderData, source, cell, sheets);

        if (sheet.Texture is null) {
            var texture = TextureCache.Enabled ? TextureCache.TryLoad(sheet, source) : null;
            if (texture is null) {
                texture = new(sheet.TextureWidth, sheet.TextureHeight, source.format, source.mipmapCount > 1);
                texture.SetPixels32(new Color32[sheet.TextureWidth * sheet.TextureHeight]);
            }

            texture.name = sheet.TextureName;
            texture.filterMode = source.filterMode;
            sheet.Texture = texture;
            sheet.Material.SetTexture(MainTex, texture);
        }

        row._idRenderData = sheet.RenderData.name;
        row.renderData = sheet.RenderData;

        AddVanillaTiles(row.tiles, source, sheet, cell);
        AddVanillaTiles(row.tiles_snow, source, sheet, cell);

        var columns = (int)sheet.RenderData.pass.pmesh.tiling.x;
        foreach (var rule in rules) {
            if (!_slots.TryGetValue(rule.ConcatFilePath, out var slot)) {
                _usedSlots.Clear();
                slot = FindSlot(cell, sheet.RemainTiles, _usedSlots);
                if (!sheet.UseCache) {
                    TileCompositor.Blit(rule.Layers, source, sheet, 0, slot, cell.x, cell.y);
                }

                _slots[rule.ConcatFilePath] = slot;
                sheet.RemainTiles.ExceptWith(_usedSlots);
            }

            rule.ReplacedTile = slot / 100 * columns + slot % 100;
        }
    }

    internal static void ReplaceTiles(string charaId)
    {
        if (!EClass.sources.charas.map.TryGetValue(charaId, out var row)) {
            TextureExpand.LogError("Not found Chara: " + charaId);
            return;
        }

        var originals = row.tiles.Concat(row.tiles_snow).ToList();
        Replace(row.tiles);
        Replace(row.tiles_snow);

        row._tiles = [];
        row._tiles_snow = [];
        row.SetTiles();

        if (!TileOrigins.TryGetValue(row._idRenderData, out var origins)) {
            TileOrigins[row._idRenderData] = origins = new();
        }

        var count = row._tiles.Length;
        for (var i = 0; i < originals.Count; i++) {
            if (i < count) {
                origins[row._tiles[i]] = originals[i];
            } else if (i - count < row._tiles_snow.Length) {
                origins[row._tiles_snow[i - count]] = originals[i];
            }
        }
    }

    private static void Replace(int[] tiles)
    {
        for (var i = 0; i < tiles.Length; i++) {
            if (_slots.TryGetValue(tiles[i].ToString(), out var slot)) {
                tiles[i] = slot;
            } else {
                TextureExpand.LogError($"Not found tileId: {tiles[i]}");
            }
        }
    }

    private static RenderData? LoadSource(string id)
    {
        if (!_sources.TryGetValue(id, out var data)) {
            _sources[id] = data = ResourceCache.Load<RenderData>(RenderDataPath + id);
        }

        return data;
    }

    private static (string Name, string? Option, RenderData Instance) GetBase(SourceChara.Row row)
    {
        if (_bases.TryGetValue(row.idRenderData, out var cached)) {
            return cached;
        }

        var parts = row.idRenderData.Replace("@", "").Split('#');
        var name = parts[0];
        if (name.Contains(ExpandSuffix)) {
            name = name[..name.IndexOf(ExpandSuffix, StringComparison.Ordinal)];
        }

        var option = parts.Length > 1 ? "#" + parts[1] : null;
        var instance = (ResourceCache.Load<RenderData>(row.pathRenderData + name) ?? row.defaultRenderData).Instantiate();
        return _bases[row.idRenderData] = (name, option, instance);
    }

    private static bool HasRoom(CustomRenderData sheet, Vector2Int cell, int needed)
    {
        _usedSlots.Clear();
        for (var i = 0; i < needed; i++) {
            if (FindSlot(cell, sheet.RemainTiles, _usedSlots) < 0) {
                return false;
            }
        }

        return true;
    }

    private static CustomRenderData CreateSheet(SourceChara.Row row,
                                                string name,
                                                string? option,
                                                RenderData baseData,
                                                Texture2D source,
                                                Vector2Int cell,
                                                List<CustomRenderData> sheets)
    {
        var textureName = name + ExpandSuffix + sheets.Count;
        var dataName = (row.idRenderData.StartsWith("@") ? "@" : "") + textureName + option;

        var data = baseData.Instantiate();
        data.name = dataName;
        ResourceCache.caches.dict.Add(nameof(RenderData) + row.pathRenderData + dataName, data);

        var basePass = data.pass;
        var pass = basePass.Instantiate();
        pass.name = "pass " + dataName;
        data.pass = pass;

        var pmesh = pass.pmesh = basePass.pmesh.Instantiate();
        var material = pass.mat = new(basePass.mat);
        _passes.Add(pass);

        if (pass.subPass != null) {
            var sub = basePass.subPass.Instantiate();
            sub.name = $"pass {dataName} sub";
            sub.pmesh = pmesh;
            sub.mat = material;
            pass.subPass = sub;
            _passes.Add(sub);
        }

        if (pass.snowPass != null) {
            var snow = basePass.snowPass.Instantiate();
            snow.name = $"pass {dataName} snow";
            snow.pmesh = pmesh;
            snow.mat = material;
            pass.snowPass = snow;
            _passes.Add(snow);
        }

        if (pass.shadowPass != null) {
            var shadow = basePass.shadowPass.Instantiate();
            shadow.name = $"pass {dataName} shadow";
            shadow.pmesh = pmesh;
            shadow.mat = material;
            pass.shadowPass = shadow;
            _passes.Add(shadow);
        }

        var columns = source.width / cell.x;
        var rows = source.height / cell.y;
        var slots = new SortedSet<int>();
        for (var r = 0; r < rows; r++) {
            for (var c = 0; c < columns; c++) {
                slots.Add(c + r * 100);
            }
        }

        var sheet = new CustomRenderData(data, textureName, source.width, source.height, material, slots);
        sheets.Add(sheet);
        return sheet;
    }

    private static void AddVanillaTiles(int[] tiles, Texture2D source, CustomRenderData sheet, Vector2Int cell)
    {
        foreach (var tile in tiles) {
            var key = tile.ToString();
            if (_slots.ContainsKey(key)) {
                continue;
            }

            _usedSlots.Clear();
            var slot = FindSlot(cell, sheet.RemainTiles, _usedSlots);
            if (!sheet.UseCache) {
                TileCompositor.Blit(null, source, sheet, tile, slot, cell.x, cell.y);
            }

            _slots[key] = slot;
            sheet.RemainTiles.ExceptWith(_usedSlots);
        }
    }

    private static int FindSlot(Vector2Int cell, SortedSet<int> remain, HashSet<int> used)
    {
        var rows = cell.y / 128;
        var found = -1;
        foreach (var slot in remain) {
            if (used.Contains(slot)) {
                continue;
            }

            var fits = true;
            for (var i = 1; i < rows; i++) {
                var above = slot + i * 100;
                if (!remain.Contains(above) || used.Contains(above)) {
                    fits = false;
                    break;
                }
            }

            if (!fits) {
                continue;
            }

            found = slot;
            break;
        }

        if (found >= 0) {
            for (var i = 0; i < rows; i++) {
                used.Add(found - i * 100);
            }
        }

        return found;
    }
}