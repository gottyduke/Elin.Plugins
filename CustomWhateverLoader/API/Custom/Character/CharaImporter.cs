using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.Extensions;
using Cwl.LangMod;
using Cwl.Patches;
using MethodTimer;

namespace Cwl.API.Custom;

public partial class CustomChara
{
    public static bool ValidateZone(string zoneFullName, out Zone? zone, bool randomFallback = false)
    {
        var zones = game.spatials.map.Values
            .OfType<Zone>()
            .ToArray();

        var (matchZone, byLv) = ParseZoneFullName(zoneFullName);
        var byId = matchZone.Replace("Zone_", "");

        zone = zones.FirstOrDefault(z => z.GetType().Name == matchZone || z.id == byId)?.FindOrCreateZone(byLv);

        if (zone is not null) {
            return true;
        }

        if (byId != "*" && !randomFallback) {
            return false;
        }

        var spawnableZones = Array.FindAll(zones, z => z.CanSpawnAdv);
        zone = spawnableZones.RandomItem();

        return zone is not null;
    }

    private static (string, int) ParseZoneFullName(string zoneFullName)
    {
        var byLv = zoneFullName.LastIndexOf('/');
        if (byLv == -1 || byLv >= zoneFullName.Length - 1) {
            return (zoneFullName.Replace("/", ""), 0);
        }

        var lv = zoneFullName[(byLv + 1)..];
        return (
            zoneFullName[..byLv],
            lv.AsInt(0)
        );
    }

    public static void SpawnAtZone(Chara chara, string zoneFullName)
    {
        if (!ValidateZone(zoneFullName, out var destZone, true) || destZone is null) {
            return;
        }

        SpawnAtZone(chara, destZone);
    }

    public static void SpawnAtZone(Chara chara, Zone zone)
    {
        // credits to 105gun
        chara.SetHomeZone(zone);
        chara.global.transition = new() {
            state = ZoneTransition.EnterState.RandomVisit,
        };
        zone.AddCard(chara);
    }

    [Time]
    internal static void AddDelayedChara()
    {
        var listAdv = game.cards.listAdv;
        var charas = game.cards.globalCharas.Values
            .GroupBy(c => c.id)
            .ToDictionary(cg => cg.Key, cg => new HashSet<Zone>(cg.Select(c => c.homeZone ?? c.currentZone)));

        foreach (var (id, import) in _delayedCharaImport) {
            if (!SafeSceneInitPatch.SafeToCreate) {
                return;
            }

            charas.TryAdd(id, []);

            try {
                var isAdv = import.Type is ImportType.Adventurer;

                var skipLoc = isAdv ? "cwl_log_skipped_adv" : "cwl_log_skipped_cm";
                var addLoc = isAdv ? "cwl_log_added_adv" : "cwl_log_added_cm";

                List<Zone> toAddZones = [];
                var present = 0;
                foreach (var toImport in import.Zones) {
                    if (!ValidateZone(toImport, out var zone, true) || zone is null) {
                        CwlMod.WarnWithPopup<CustomChara>("cwl_error_zone_invalid".Loc(id, toImport));
                        continue;
                    }

                    if (!charas[id].Contains(zone)) {
                        toAddZones.Add(zone);
                    } else {
                        present++;
                        CwlMod.Log<CustomChara>(skipLoc.Loc(id));
                    }
                }

                for (var i = 0; i < import.Zones.Length - present; ++i) {
                    var toAddZone = toAddZones[i];

                    if (isAdv) {
                        if (game.cards.globalCharas.Find(id) is not null) {
                            CwlMod.Log<CustomChara>(skipLoc.Loc(id));
                            break;
                        }

                        toAddZone = toAddZones[0];
                    }

                    if (!CreateTaggedChara(id, out var chara, import) || chara is null) {
                        break;
                    }

                    SpawnAtZone(chara, toAddZone);

                    if (isAdv) {
                        listAdv.Add(chara);
                    }

                    CwlMod.Log<CustomChara>(addLoc.Loc(id, chara.homeZone.Name));
                }
            } catch (Exception ex) {
                CwlMod.WarnWithPopup<CustomChara>("cwl_error_failure".Loc(ex.Message), ex);
                // noexcept
            }
        }
    }

    // 1.18 allow duplicate import based on zone
    public record CharaImport(ImportType Type, string[] Zones, string[] Equips, string[] Things);
}