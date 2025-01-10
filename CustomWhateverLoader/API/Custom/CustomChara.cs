using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using Cwl.Patches;
using MethodTimer;

namespace Cwl.API.Custom;

public class CustomChara : Chara
{
    public enum ImportType
    {
        Commoner,
        Adventurer,
        Merchant,
    }

    private static readonly Dictionary<string, CharaImport> _delayedCharaImport = [];
    internal static readonly Dictionary<string, string> DramaRoute = [];
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

        var zone = "!";
        List<string> equips = [];
        List<string> things = [];

        foreach (var tag in tags) {
            var @params = tag.Split('_');
            var action = @params[0];
            switch (action) {
                case "AdvZone":
                case "Zone":
                    zone = tag[(action.Length + 1)..];
                    break;
                case "AdvEq":
                case "Eq":
                    equips.Add(tag[(action.Length + 1)..]);
                    break;
                case "AdvThing":
                case "Thing":
                    things.Add(tag[(action.Length + 1)..]);
                    break;
                case "Stock":
                    CustomMerchant.AddStock(r.id);
                    break;
                case "Drama":
                    var drama = tag[(action.Length + 1)..];
                    if (drama is not "" &&
                        PackageIterator.GetRelocatedFilesFromPackage($"Dialog/Drama/{drama}.xlsx").Any()) {
                        DramaRoute[r.id] = drama;
                    }

                    break;
                case "Bio":
                    var bio = @params[1];
                    var data = PackageIterator.GetRelocatedFilesFromPackage($"Data/bio_{bio}.json").ToArray();
                    if (bio is not "" && data.Length > 0) {
                        BioOverride[r.id] = data[0].FullName;
                    }

                    break;
            }
        }

        _delayedCharaImport.Add(r.id, new(import, zone, equips.ToArray(), things.ToArray()));
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
        } catch (Exception ex) {
            CwlMod.Error(ex);
            // noexcept
        }

        chara?.Destroy();
        chara = null;

        CwlMod.Error("cwl_error_chara_gen".Loc(id));
        return false;
    }

    public static bool CreateTaggedChara(string id, out Chara? chara, CharaImport import)
    {
        return CreateTaggedChara(id, out chara, import.Equips, import.Things);
    }

    [Time]
    internal static IEnumerator AddDelayedChara()
    {
        var delayed = _delayedCharaImport.Count;
        while (SafeSceneInitPatch.SafeToCreate && delayed > 0) {
            foreach (var (id, import) in _delayedCharaImport) {
                delayed--;

                try {
                    if (import.Zone == "!") {
                        continue;
                    }

                    var listAdv = game.cards.listAdv;
                    var duplicate = game.cards.globalCharas.Values.FirstOrDefault(c => c.id == id);
                    if (duplicate is not null) {
                        if (import.Type == ImportType.Adventurer) {
                            if (listAdv.FirstOrDefault(c => c.id == id) is not null) {
                                CwlMod.Log("cwl_log_skipped_adv".Loc(id));
                                // regression as of 1.16.5
                                if (duplicate.homeZone is Zone_Dungeon) {
                                    var newHome = game.world.region.ListTowns().RandomItem();
                                    duplicate.SetHomeZone(newHome);
                                    duplicate.global.transition = new() {
                                        state = ZoneTransition.EnterState.RandomVisit,
                                    };
                                    newHome.AddCard(duplicate);
                                }
                            } else {
                                listAdv.Add(duplicate);
                                CwlMod.Log("cwl_log_added_adv".Loc(id, duplicate.homeZone.Name));
                            }
                        } else {
                            CwlMod.Log("cwl_log_skipped_cm".Loc(id));
                        }

                        continue;
                    }

                    if (!CreateTaggedChara(id, out var chara, import) || chara is null) {
                        continue;
                    }

                    // credits to 105gun
                    var zones = game.spatials.map.Values
                        .OfType<Zone>()
                        .ToArray();
                    var homeZone = zones.FirstOrDefault(z => z is not Zone_Dungeon &&
                                                             z.GetType().Name.Split("_")[^1] == import.Zone) ??
                                   zones.Where(z => z.CanSpawnAdv).RandomItem();

                    chara.SetHomeZone(homeZone);
                    chara.global.transition = new() {
                        state = ZoneTransition.EnterState.RandomVisit,
                    };
                    homeZone.AddCard(chara);

                    switch (import.Type) {
                        case ImportType.Adventurer:
                            listAdv.Add(chara);
                            CwlMod.Log("cwl_log_added_adv".Loc(id, homeZone.Name));
                            break;
                        case ImportType.Merchant:
                        case ImportType.Commoner:
                        default:
                            CwlMod.Log("cwl_log_added_cm".Loc(id, homeZone.Name));
                            break;
                    }
                } catch {
                    // noexcept
                }
            }

            yield return null;
        }
    }

    private static void AddEqOrThing(Chara chara, string id, string? payload, bool equip = false)
    {
        if (sources.cards.map.TryGetValue(id) is null) {
            CwlMod.Warn("cwl_warn_thing_gen".Loc(id, chara.id));
            return;
        }

        Thing? thing = null;
        try {
            if (equip) {
                if (!Enum.TryParse<Rarity>(payload, true, out var rarity)) {
                    rarity = Rarity.Random;
                }

                thing = chara.EQ_ID(id, r: rarity);
                thing.c_IDTState = 0;
                if (!thing.isDestroyed) {
                    thing.ChangeRarity(rarity);
                    CwlMod.Log("cwl_log_added_eq".Loc(id, thing.rarity.ToString(), chara.id));
                    return;
                }
            }

            if (!int.TryParse(payload, out var count)) {
                count = 1;
            }

            thing = ThingGen.Create(id).SetNum(count);
            thing.c_IDTState = 0;
            chara.AddThing(thing);

            CwlMod.Log("cwl_log_added_thing".Loc(id, thing.Num, chara.id));
        } catch {
            thing?.Destroy();
            CwlMod.Warn("cwl_warn_thing_gen".Loc(id, chara.id));
            // noexcept
        }
    }

    public record CharaImport(ImportType Type, string Zone, string[] Equips, string[] Things);
}