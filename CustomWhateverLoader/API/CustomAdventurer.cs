using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.String;
using Cwl.LangMod;
using Cwl.Loader;
using Cwl.Loader.Patches;
using MethodTimer;

namespace Cwl.API;

public class CustomAdventurer
{
    private static readonly Dictionary<string, HashSet<string>> _delayedCharaImport = [];

    public static IEnumerable<string> All => _delayedCharaImport.Keys;

    [Time]
    public static void AddAdventurer(string charaId, params string[] tags)
    {
        _delayedCharaImport.TryAdd(charaId, []);
        foreach (var tag in tags) {
            _delayedCharaImport[charaId].Add(tag);
        }
    }

    public static Chara CreateTaggedChara(string charaId)
    {
        var adventurer = CharaGen.Create(charaId);
        adventurer.RemoveThings();
        return adventurer;
    }

    [Time]
    internal static IEnumerator AddDelayedChara()
    {
        var remain = _delayedCharaImport.Count;
        while (SafeSceneInitPatch.SafeToCreate && remain > 0) {
            foreach (var (id, tags) in _delayedCharaImport) {
                remain--;

                try {
                    var chara = CreateTaggedChara(id);
                    if (chara.id == "beggar") {
                        CwlMod.Error("cwl_error_chara_gen".Loc(id));
                        chara.Destroy();
                        continue;
                    }

                    // credits to 105gun
                    var towns = EMono.game.world.region.ListTowns();
                    foreach (var tag in tags) {
                        var @params = tag.Parse("#", 3);
                        var payload = @params[0];

                        if (payload.StartsWith("Zone_")) {
                            var duplicate = EMono.game.cards.listAdv.FirstOrDefault(c => c.id == id);
                            if (duplicate is not null) {
                                if (@params[1] != "Replace") {
                                    CwlMod.Log("cwl_log_skipped_adv".Loc(id));
                                    chara.Destroy();
                                    break;
                                }

                                EMono.game.cards.listAdv.Remove(duplicate);
                            }

                            var zone = towns.FirstOrDefault(t => t.GetType().Name == tag);
                            if (payload.EndsWith("*") || zone is null) {
                                zone = towns.RandomItem();
                            }

                            chara.SetHomeZone(zone);
                            chara.global.transition = new() {
                                state = ZoneTransition.EnterState.RandomVisit,
                            };

                            zone.AddCard(chara);
                            EMono.game.cards.listAdv.Add(chara);

                            CwlMod.Log("cwl_log_added_adv".Loc(id, zone.GetType().Name));

                            continue;
                        }

                        if (payload.StartsWith("Eq_") || payload.StartsWith("Thing_")) {
                            var thingId = payload.StartsWith("Eq_") ? payload[3..] : payload[6..];
                            var doEquip = payload.StartsWith("Eq_");
                            if (thingId is "") {
                                continue;
                            }

                            var thing = EMono.sources.cards.map.TryGetValue(thingId);
                            if (thing is null) {
                                CwlMod.Warn("cwl_warn_thing_gen".Loc(thingId, id));
                                continue;
                            }

                            if (doEquip) {
                                var rarity = Rarity.Random;
                                if (Enum.TryParse<Rarity>(@params[1], true, out var rarityEnum)) {
                                    rarity = rarityEnum;
                                }

                                var equip = chara.EQ_ID(thingId, r: rarity);
                                if (!chara.things.Contains(equip) && !equip.isDestroyed) {
                                    chara.AddThing(equip);
                                }

                                CwlMod.Log("cwl_log_added_eq".Loc(thingId, Enum.GetName(typeof(Rarity), rarity)!, id));
                            } else {
                                int.TryParse(@params[1], out var count);
                                count = count is 0 ? 1 : count;

                                chara.AddThing(ThingGen.Create(thingId).SetNum(count));
                                CwlMod.Log("cwl_log_added_thing".Loc(thingId, count, id));
                            }

                            continue;
                        }

                        if (payload.StartsWith("Placeholder")) {
                        }
                    }
                } catch {
                    // noexcept
                }
            }

            yield return null;
        }
    }
}