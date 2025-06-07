using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.Exceptions;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using MethodTimer;
using ReflexCLI.Attributes;

namespace Cwl.API.Custom;

[ConsoleCommandClassCustomizer("cwl")]
public partial class CustomChara : Chara
{
    public enum ImportType
    {
        Commoner,
        Adventurer,
        Merchant,
        Unique,
    }

    private static readonly Dictionary<string, ImportType> _cachedTraitTypes = [];
    private static readonly Dictionary<string, CharaImport> _delayedCharaImport = [];
    internal static readonly Dictionary<string, string> DramaRoutes = [];
    internal static readonly Dictionary<string, string> BioOverride = [];

    public static IReadOnlyCollection<string> All => _delayedCharaImport.Keys;

    [Time]
    public static void AddChara(SourceChara.Row r)
    {
        var tags = r.tag
            .Select(t => t.Trim())
            .Where(t => t.StartsWith("add"))
            .Select(t => t[3..])
            .ToArray();
        if (tags.Length == 0) {
            return;
        }

        var trait = r.trait.TryGet(0, true) ?? "Chara";
        if (!_cachedTraitTypes.TryGetValue(trait, out var import)) {
            var traitType = ClassCache.Create<Trait>($"{nameof(Trait)}{trait}", "Elin");
            _cachedTraitTypes[trait] = import = traitType switch {
                TraitAdventurer => ImportType.Adventurer,
                TraitMerchant => ImportType.Merchant,
                TraitUniqueChara => ImportType.Unique,
                _ => ImportType.Commoner,
            };
        }

        List<string> zones = [];
        List<string> equips = [];
        List<string> things = [];

        foreach (var tag in tags) {
            var sanitized = tag.StartsWith("Adv") ? tag[3..] : tag;
            var @params = sanitized.Split('_');
            var action = @params[0];
            switch (action) {
                case "Zone":
                    zones.Add(sanitized);
                    break;
                case "Eq":
                    if (@params.Length > 1) {
                        equips.Add(sanitized[(action.Length + 1)..]);
                    }

                    break;
                case "Thing":
                    if (@params.Length > 1) {
                        things.Add(sanitized[(action.Length + 1)..]);
                    }

                    break;
                case "Stock":
                    CustomMerchant.AddStock(r.id, sanitized[action.Length..].TrimStart('_'));
                    break;
                case "Drama":
                    if (@params.Length <= 1) {
                        break;
                    }

                    var drama = sanitized[(action.Length + 1)..];
                    if (drama != "" &&
                        PackageIterator.GetRelocatedFilesFromPackage($"Dialog/Drama/{drama}.xlsx").Any()) {
                        DramaRoutes[r.id] = drama;
                    }

                    break;
                case "Bio":
                    if (@params.Length <= 1) {
                        break;
                    }

                    var bio = sanitized[(action.Length + 1)..];
                    var data = PackageIterator.GetRelocatedFilesFromPackage($"Data/bio_{bio}.json").ToArray();
                    if (bio != "" && data.Length > 0) {
                        BioOverride[r.id] = data[^1].FullName;
                    }

                    break;
            }
        }

        _delayedCharaImport[r.id] = new(import, zones.ToArray(), equips.ToArray(), things.ToArray());
    }

    public static bool CreateTaggedChara(string id, out Chara? chara)
    {
        chara = null;
        if (!sources.charas.map.TryGetValue(id, out var row)) {
            return false;
        }

        AddChara(row);
        return _delayedCharaImport.TryGetValue(id, out var import) && CreateTaggedChara(id, out chara, import);
    }

    public static bool CreateTaggedChara(string id, out Chara? chara, CharaImport import)
    {
        return CreateTaggedChara(id, out chara, import.Equips, import.Things);
    }

    public static bool CreateTaggedChara(string id, out Chara? chara, string[]? equips, string[]? things = null)
    {
        chara = null;

        try {
            chara = CharaGen.Create(id);
            // 23.149 changed beggar to chicken, what noa
            if (chara.id == "chicken") {
                throw new BeggarException(id);
            }

            equips ??= [];
            things = [
                ..equips,
                ..things ?? [],
            ];

            if (things.Length <= 0) {
                return true;
            }

            chara.RemoveThings();

            for (var i = 0; i < things.Length; ++i) {
                var @params = things[i].Parse("#", 2);
                var doEquip = i < equips.Length;

                AddEqOrThing(chara, @params[0]!, @params[1], doEquip);
            }

            return true;
        } catch (Exception ex) {
            chara?.Destroy();
            chara = null;

            CwlMod.ErrorWithPopup<CustomChara>("cwl_error_chara_gen".Loc(id), ex);
            return false;
            // noexcept
        }
    }

    [ConsoleCommand("spawn")]
    public static string SpawnTagged(string id)
    {
        if (!CreateTaggedChara(id, out var chara) || chara is null) {
            return "uwu failed";
        }

        _zone.AddCard(chara, pc.pos);
        return chara.Name;
    }
}