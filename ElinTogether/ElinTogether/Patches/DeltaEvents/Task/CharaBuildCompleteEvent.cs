using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class CharaBuildCompleteEvent
{
    internal static bool Building { get; private set; }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TaskBuild), nameof(TaskBuild.OnProgressComplete))]
    internal static bool OnProgressComplete_Patch(TaskBuild __instance)
    {
        OnProgressComplete_Before(__instance);
        OnProgressComplete_Modified(__instance);
        return false;
    }

    internal static void OnProgressComplete_Before(TaskBuild taskBuild)
    {
        if (taskBuild.owner is null || taskBuild.held is null) {
            return;
        }

        if (NetSession.Instance.Connection is not { } connection) {
            return;
        }

        // drop all other task completed and wait for delta
        if (!connection.IsHost && !taskBuild.owner.IsPC) {
            return;
        }

        connection.Delta.AddRemote(new CharaBuildDelta {
            Held = taskBuild.held,
            Owner = taskBuild.owner,
            Pos = taskBuild.pos,
            Dir = taskBuild.recipe._dir,
            Altitude = taskBuild.altitude,
            BridgeHeight = taskBuild.bridgeHeight,
        });
    }

    internal static void OnProgressComplete_Modified(TaskBuild thiz)
    {
        Building = true;
        OnProgressComplete_ModifiedInner(thiz);
        Building = false;
    }

    internal static void OnProgressComplete_ModifiedInner(TaskBuild thiz)
    {
        if (thiz.useHeld) {
            if (thiz.owner.held == null || thiz.owner.held.GetRootCard() != thiz.owner || thiz.pos.Distance(thiz.owner.pos) > 1 ||
                !thiz.pos.IsInBounds) {
                return;
            }

            if (thiz.CanRotateBlock()) {
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
            thiz.pos.ForeachMultiSize(thiz.recipe.W, thiz.recipe.H, delegate(Point p, bool center) {
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
}