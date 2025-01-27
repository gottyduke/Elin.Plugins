using System;
using System.Linq;
using Cwl.Helper.Extensions;
using Cwl.LangMod;
using Cwl.Patches;
using MethodTimer;

namespace Cwl.API.Custom;

public partial class CustomChara
{
    private static Zone[]? _zones;

    public static bool ValidateZone(string zoneFullName, out Zone? zone, bool randomFallback = false)
    {
        _zones ??= game.spatials.map.Values
            .OfType<Zone>()
            .ToArray();

        var matchName = zoneFullName;
        zone = Array.Find(_zones, z => z is not Zone_Dungeon && z.GetType().Name == matchName);
        if (zone is null && zoneFullName != "Zone_*" && !randomFallback) {
            return false;
        }

        zone ??= Array.FindAll(_zones, z => z.CanSpawnAdv).RandomItem();
        return zone is not null;
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

                    string? invalidZone = null;
                    if (!ValidateZone(toAddZone, out var destZone) || destZone is null) {
                        invalidZone = toAddZone;
                        toAddZone = "Zone_*";
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

                    if (invalidZone is not null) {
                        CwlMod.WarnWithPopup<CustomChara>("cwl_error_zone_invalid".Loc(id, invalidZone));
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
                CwlMod.WarnWithPopup<CustomChara>("cwl_error_failure".Loc(ex.Message), ex);
                // noexcept
            }
        }

        // purge beggars
        listAdv.RemoveAll(c => c.id == "beggar");
    }

    // 1.18 allow duplicate import based on zone
    public record CharaImport(ImportType Type, string[] Zones, string[] Equips, string[] Things);
}