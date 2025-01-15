using System;
using System.Linq;
using Cwl.Helper.Extensions;
using Cwl.LangMod;
using Cwl.Patches;
using MethodTimer;

namespace Cwl.API.Custom;

public partial class CustomChara
{
    public static void SpawnAtZone(Chara chara, string zoneFullName)
    {
        // credits to 105gun
        var zones = game.spatials.map.Values
            .OfType<Zone>()
            .ToArray();
        var destZone = Array.Find(zones, z => z is not Zone_Dungeon && z.GetType().Name == zoneFullName) ??
                       Array.FindAll(zones, z => z.CanSpawnAdv).RandomItem();

        chara.SetHomeZone(destZone);
        chara.global.transition = new() {
            state = ZoneTransition.EnterState.RandomVisit,
        };
        destZone.AddCard(chara);
    }

    [Time]
    internal static void AddDelayedChara()
    {
        var listAdv = game.cards.listAdv;
        var charas = game.cards.globalCharas.Values.ToLookup(c => c.id, c => (c.homeZone ?? c.currentZone)?.GetType().Name);

        foreach (var (id, import) in _delayedCharaImport) {
            if (!SafeSceneInitPatch.SafeToCreate) {
                return;
            }

            try {
                var skipLoc = import.Type == ImportType.Adventurer ? "cwl_log_skipped_adv" : "cwl_log_skipped_cm";
                var addZones = import.Zones
                    .MissingFrom(charas[id], _ => CwlMod.Log<CustomChara>(skipLoc.Loc(id)))
                    .OfType<string>();

                foreach (var zone in addZones) {
                    var toAddZone = zone;

                    if (import.Type == ImportType.Adventurer) {
                        if (game.cards.globalCharas.Find(id) is { } exist) {
                            if (listAdv.Find(c => c.id == id) is null) {
                                // register exist chara as adv
                                listAdv.Add(exist);
                                CwlMod.Log<CustomChara>("cwl_log_added_adv".Loc(id, (exist.homeZone ?? exist.currentZone).Name));
                            }

                            CwlMod.Log<CustomChara>(skipLoc.Loc(id));
                            break;
                        }

                        toAddZone = import.Zones[0];
                    }

                    if (toAddZone.EndsWith("*")) {
                        if (game.cards.globalCharas.Values.Count(c => c.id == id) >= import.Zones.Length) {
                            CwlMod.Log<CustomChara>(skipLoc.Loc(id));
                            break;
                        }
                    }

                    if (!CreateTaggedChara(id, out var chara, import) || chara is null) {
                        break;
                    }

                    SpawnAtZone(chara, toAddZone);

                    var loc = "cwl_log_added_cm";
                    if (import.Type == ImportType.Adventurer) {
                        listAdv.Add(chara);
                        loc = "cwl_log_added_adv";
                    }

                    CwlMod.Log<CustomChara>(loc.Loc(id, chara.homeZone.Name));
                }
            } catch (Exception ex) {
                CwlMod.Warn<CustomChara>("cwl_error_failure".Loc(ex));
                // noexcept
            }
        }

        // purge beggars
        listAdv.RemoveAll(c => c.id == "beggar");
    }

    // 1.18 allow duplicate import based on zone
    public record CharaImport(ImportType Type, string[] Zones, string[] Equips, string[] Things);
}