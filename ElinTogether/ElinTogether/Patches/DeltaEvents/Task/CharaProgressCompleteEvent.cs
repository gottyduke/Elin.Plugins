using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.Helper;
using ElinTogether.Helper;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class CharaProgressCompleteEvent
{
    internal static List<ElinDelta> DeltaList = [];
    internal static Chara? Chara { get; private set; }
    internal static bool IsHappening { get; private set; }
    internal static AIAct? Action { get; private set; }

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer
            .FindAllOverrides(typeof(AIAct), nameof(AIAct.OnProgressComplete))
            .Where(mi => typeof(AIProgress).IsAssignableFrom(mi.DeclaringType) || mi.DeclaringType == typeof(TaskBuild));
    }

    [HarmonyPrefix]
    internal static bool OnProgressComplete(AIAct __instance)
    {
        if (NetSession.Instance.Connection is not { } connection || __instance.owner is null) {
            return true;
        }

        Chara = __instance.owner;
        Action = __instance;
        IsHappening = true;

        if (__instance is not TaskBuild taskBuild) {
            return true;
        }

        if (Chara.IsPC && !ElinDelta.IsApplying) {
            SendCharaBuildDelta(taskBuild);
        }

        if (connection.IsHost || ElinDelta.IsApplying) {
            OnTaskBuildComplete(taskBuild);
        }

        IsHappening = false;
        return false;
    }

    [HarmonyPostfix]
    internal static void OnProgressCompleteEnd(AIAct __instance)
    {
        Chara = null;
        Action = null;
        IsHappening = false;

        if (__instance.owner is null) {
            return;
        }

        // only host can complete progress
        if (NetSession.Instance.Connection is not ElinNetHost connection) {
            return;
        }

        if (__instance is TaskBuild) {
            return;
        }

        // due to randomness in max progress
        // remote needs to be notified that a remote task is completed before starting anew
        connection.Delta.AddRemote(new CharaProgressCompleteDelta {
            Owner = __instance.owner,
            CompletedActId = SourceValidation.ActToIdMapping[__instance.parent.GetType()],
            DeltaList = [.. DeltaList],
        });

        DeltaList.Clear();
    }

    internal static void SendCharaBuildDelta(TaskBuild taskBuild)
    {
        if (taskBuild.held is null) {
            return;
        }

        NetSession.Instance.Connection!.Delta.AddRemote(new CharaBuildDelta {
            Held = taskBuild.held,
            Owner = taskBuild.owner,
            Pos = taskBuild.pos,
            Dir = taskBuild.recipe._dir,
            Altitude = taskBuild.altitude,
            BridgeHeight = taskBuild.bridgeHeight,
        });
    }

    internal static void OnTaskBuildComplete(TaskBuild thiz)
    {
        if (thiz.useHeld) {
            if (thiz.owner.held == null || thiz.owner.held.GetRootCard() != thiz.owner || thiz.pos.Distance(thiz.owner.pos) > 1 ||
                !thiz.pos.IsInBounds) {
                return;
            }

            if (CanRotateBlock(thiz)) {
                SE.Rotate();
                thiz.pos.cell.RotateBlock(1);
                thiz.disableRotateBlock = true;
                return;
            }

            thiz.disableRotateBlock = true;
            ActionMode.Build.FixBridge(thiz.pos, thiz.recipe);
            thiz.bridgeHeight = ActionMode.Build.bridgeHeight;
            thiz.target = thiz.owner.held.category.installOne ? thiz.owner.held.Split(1) : thiz.owner.held;
            if (thiz.target.trait is TraitTile) {
                thiz.target.ModNum(-1);
            }

            thiz.dir = thiz.recipe._dir;
            thiz.owner.LookAt(thiz.pos);
            thiz.owner.renderer.PlayAnime(AnimeID.Attack_Place, thiz.pos);
            if (thiz.target.id == "statue_weird") {
                thiz.owner.Say("statue_install");
            }
        }

        thiz.lastPos = thiz.pos.Copy();
        if (ActionMode.Build.IsActive && ActionMode.Build.IsFillMode()) {
            if (thiz.recipe.IsBridge) {
                thiz.dir = thiz.pos.cell.floorDir;
                thiz.bridgeHeight = thiz.pos.cell.bridgeHeight;
                thiz.altitude = 0;
            } else if (thiz.recipe.IsFloor) {
                thiz.dir = thiz.pos.cell.floorDir;
            } else if (thiz.recipe.IsBlock) {
                thiz.dir = thiz.pos.cell.blockDir;
            }
        } else {
            Effect.Get("smoke").Play(thiz.pos);
            Effect.Get("mine").Play(thiz.pos).SetParticleColor(thiz.recipe.GetColorMaterial().GetColor())
                .Emit(10 + EClass.rnd(10));
            if (thiz.recipe.IsWallOrFence) {
                if (thiz.pos.HasWallOrFence && thiz.pos.cell.blockDir != 2 && thiz.pos.cell.blockDir != thiz.recipe._dir) {
                    thiz.pos.cell.blockDir = 2;
                    thiz.owner.PlaySound(thiz.pos.matBlock.GetSoundImpact());
                    thiz.pos.RefreshTile();
                    return;
                }

                if (thiz.pos.sourceRoofBlock.tileType.IsWallOrFence && thiz.pos.cell._roofBlockDir % 4 != 2 &&
                    thiz.pos.cell._roofBlockDir % 4 != thiz.recipe._dir) {
                    thiz.pos.cell._roofBlockDir = (byte)(thiz.pos.cell._roofBlockDir / 4 * 4 + 2);
                    thiz.owner.PlaySound(thiz.pos.matBlock.GetSoundImpact());
                    thiz.pos.RefreshTile();
                    return;
                }
            }
        }

        if (thiz.bridgeHeight > 150) {
            thiz.bridgeHeight = 150;
        }

        thiz.recipe.Build(thiz);
        thiz.resources.Clear();
        EClass.player.flags.OnBuild(thiz.recipe);
        EClass._map.RefreshShadow(thiz.pos.x, thiz.pos.z);
        EClass._map.RefreshShadow(thiz.pos.x, thiz.pos.z - 1);
        EClass._map.RefreshFOV(thiz.pos.x, thiz.pos.z);
        thiz.owner.renderer.SetFirst(true);
        if (thiz.recipe.IsFloor) {
            foreach (var card in thiz.pos.ListThings<TraitNewZone>()) {
                var isDownstairs = (card.trait as TraitNewZone)!.IsDownstairs;
            }
        }

        if (!ActionMode.Build.IsActive || !ActionMode.Build.IsRoofEditMode()) {
            thiz.pos.ForeachMultiSize(thiz.recipe.W, thiz.recipe.H, delegate (Point p, bool center) {
                if (p.IsBlocked && p.HasChara) {
                    foreach (var chara in p.ListCharas()) {
                        thiz.owner.Kick(chara, true, false, false);
                    }
                }
            });
        }

        if (EClass.game.IsSurvival && EClass._zone is Zone_StartSiteSky) {
            EClass.game.survival.OnExpandFloor(thiz.pos);
        }
    }

    public static bool CanRotateBlock(TaskBuild thiz)
    {
        if (!EInput.rightMouse.pressing) {
            thiz.disableRotateBlock = false;
        }
        if (thiz.useHeld && thiz.owner.held != null) {
            if (EClass._zone.IsRegion) {
                return false;
            }
            if (EClass._zone is not Zone_Tent && !EClass._zone.IsPCFactionOrTent && thiz.owner.held.trait.CanBeOnlyBuiltInHome) {
                return false;
            }
            if (EClass._zone.RestrictBuild && !thiz.owner.held.trait.CanBuildInTown) {
                return false;
            }
            if (thiz.owner.held.trait is TraitBlock && thiz.pos.HasBlock && !thiz.owner.held.trait.IsDoor && !thiz.disableRotateBlock) {
                return true;
            }
        }
        return false;
    }
}