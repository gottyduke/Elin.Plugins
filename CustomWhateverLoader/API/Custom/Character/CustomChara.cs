using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using MethodTimer;

namespace Cwl.API.Custom;

public partial class CustomChara : Chara
{
    public enum ImportType
    {
        Commoner,
        Adventurer,
        Merchant,
    }

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

        var import = r.trait[0] switch {
            "Adventurer" or "AdventurerBacker" => ImportType.Adventurer,
            _ when r.trait[0].StartsWith("Merchant") => ImportType.Merchant,
            _ => ImportType.Commoner,
        };

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
                    CustomMerchant.AddStock(r.id, sanitized[action.Length..]);
                    break;
                case "Drama":
                    if (@params.Length <= 1) {
                        break;
                    }

                    var drama = sanitized[(action.Length + 1)..];
                    if (drama is not "" &&
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
                    if (bio is not "" && data.Length > 0) {
                        BioOverride[r.id] = data[^1].FullName;
                    }

                    break;
            }
        }

        _delayedCharaImport.Add(r.id, new(import, zones.ToArray(), equips.ToArray(), things.ToArray()));
    }

    public static bool CreateTaggedChara(string id, out Chara? chara, string[]? equips = null, string[]? things = null)
    {
        chara = null;

        try {
            chara = CharaGen.Create(id);
            if (chara.id == "beggar") {
                throw new();
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
        } catch {
            // noexcept
        }

        chara?.Destroy();
        chara = null;

        CwlMod.Error<CustomChara>("cwl_error_chara_gen".Loc(id));
        return false;
    }

    public static bool CreateTaggedChara(string id, out Chara? chara, CharaImport import)
    {
        return CreateTaggedChara(id, out chara, import.Equips, import.Things);
    }
}