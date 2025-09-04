using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.Extensions;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cwl.Patches;
using Cwl.Patches.Charas;
using MethodTimer;

namespace Cwl.API.Custom;

public partial class CustomChara
{
    public static void SpawnAtZone(Chara chara, string zoneFullName)
    {
        if (!zoneFullName.ValidateZone(out var destZone, true) || destZone is null) {
            return;
        }

        SpawnAtZone(chara, destZone);
    }

    public static void SpawnAtZone(Chara chara, Zone zone)
    {
        // credits to 105gun
        chara.homeZone = zone;
        chara.SetGlobal();
        chara.global.transition = new() {
            state = ZoneTransition.EnterState.RandomVisit,
        };
        zone.AddCard(chara);
    }

    [Time]
    [CwlSceneInitEvent(Scene.Mode.StartGame, true)]
    internal static void AddDelayedChara()
    {
        if (_deferredUntilRestoration && RestoreCharaData.Restorable.Count > 0) {
            CoroutineHelper.Deferred(AddDelayedChara, () => !_deferredUntilRestoration);
            return;
        }

        var listAdv = game.cards.listAdv;
        var charas = game.cards.globalCharas.Values.ToLookup(c => c.id);

        foreach (var (id, import) in _delayedCharaImport) {
            if (!SafeSceneInitPatch.SafeToCreate) {
                return;
            }

            try {
                var isAdv = import.Type is ImportType.Adventurer;

                var skipLoc = isAdv ? "cwl_log_skipped_adv" : "cwl_log_skipped_cm";
                var addLoc = isAdv ? "cwl_log_added_adv" : "cwl_log_added_cm";

                List<Zone> toAddZones = [];
                foreach (var toImport in import.Zones) {
                    if (toImport.ValidateZone(out var zone, true) && zone is not null) {
                        toAddZones.Add(zone);
                    } else {
                        CwlMod.WarnWithPopup<CustomChara>("cwl_error_zone_invalid".Loc(id, toImport));
                    }
                }

                var presentCharas = charas[id].ToList();
                var presentCount = presentCharas.Count;

                // adventurer is uno solo unique
                var targetCount = isAdv ? 1 : toAddZones.Count;
                var neededToSpawn = Math.Max(0, targetCount - presentCount);
                // no more
                if (neededToSpawn == 0 && presentCount > 0) {
                    CwlMod.Log<CustomChara>(skipLoc.Loc(id));
                    continue;
                }

                // adventurer already on ranking, skip
                if (isAdv && presentCount > 0) {
                    CwlMod.Log<CustomChara>(skipLoc.Loc(id));
                    continue;
                }

                for (var i = 0; i < neededToSpawn; ++i) {
                    // adventurer   -> first zone
                    // unique chara -> each zone needs an instance
                    var toAddZone = isAdv ? toAddZones.FirstOrDefault() : toAddZones.TryGet(i, true);
                    if (toAddZone is null) {
                        // no tag or invalid tag
                        break;
                    }

                    if (CreateTaggedChara(id, out var chara, import) && chara is not null) {
                        SpawnAtZone(chara, toAddZone);

                        if (isAdv) {
                            listAdv.Add(chara);
                        }

                        CwlMod.Log<CustomChara>(addLoc.Loc(id, chara.homeZone.Name));
                    } else {
                        break;
                    }
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