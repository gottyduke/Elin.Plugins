using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper;

namespace Cwl.API;

public static class CustomAdventurer
{
    internal static readonly Dictionary<string, HashSet<string>> DelayedCharaImport = [];
    internal static bool SafeToCreate = false;

    public static void AddAdventurer(string charaId, params string[] zones)
    {
        DelayedCharaImport.TryAdd(charaId, []);
        foreach (var zone in zones) {
            DelayedCharaImport[charaId].Add(zone);
        }
    }

    public static Chara CreateTaggedChara(string charaId)
    {
        var adventurer = CharaGen.Create(charaId);
        adventurer.RemoveThings();
        return adventurer;
    }

    internal static IEnumerator AddDelayedChara()
    {
        var delayed = DelayedCharaImport.Count;
        while (SafeToCreate && delayed > 0) {
            foreach (var (id, tags) in DelayedCharaImport) {
                delayed--;

                var chara = CreateTaggedChara(id);
                if (chara.id == "beggar") {
                    CwlMod.Error($"failed to add adventurer {id}, cannot be generated");
                    chara.Destroy();
                    continue;
                }

                var towns = EMono.game.world.region.ListTowns();
                foreach (var tag in tags) {
                    var @params = tag.Parse("#", 3);
                    var payload = @params[0];

                    if (payload.StartsWith("Zone_")) {
                        var duplicate = EMono.game.cards.listAdv.FirstOrDefault(c => c.id == id);
                        if (duplicate is not null) {
                            if (@params[1] != "Replace") {
                                CwlMod.Log($"skipped adventurer {id}, already exists");
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

                        CwlMod.Log($"added adventurer {id} to {zone.GetType().Name}");

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
                            CwlMod.Warn($"failed to add thing:{thingId} to {id}, cannot be generated");
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

                            CwlMod.Log(
                                $"added equipment:{thingId}, {Enum.GetName(typeof(Rarity), rarity)} to {id}");
                        } else {
                            int.TryParse(@params[1], out var count);
                            count = count is 0 ? 1 : count;

                            chara.AddThing(ThingGen.Create(thingId).SetNum(count));
                            CwlMod.Log($"added thing:{thingId}, x{count} to {id}");
                        }

                        continue;
                    }

                    if (payload.StartsWith("Placeholder")) {
                    }
                }
            }

            yield return null;
        }
    }
}