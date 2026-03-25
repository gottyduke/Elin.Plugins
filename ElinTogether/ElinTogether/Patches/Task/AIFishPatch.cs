using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ElinTogether.Helper;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches.Task;

[HarmonyPatch]
internal static class AIFishPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AI_Fish), nameof(AI_Fish.Run), MethodType.Enumerator)]
    internal static IEnumerable<CodeInstruction> OnRun(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldloc_2),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Brfalse))
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_2),
                Transpilers.EmitDelegate((AI_Fish thiz) => {
                    if (NetSession.Instance.IsClient && thiz.owner.IsPC) {
                        EClass.player.TryEquipBait();
                    }
                }))
            // if (this.owner.IsPC)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldloc_2),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Card), nameof(Card.IsPC))))
            .SetInstructionAndAdvance(
                Transpilers.EmitDelegate((Chara chara) => NetSession.Instance.IsHost ? chara.IsPlayer : chara.IsPC))
            // this.owner.TryEquipBait();
            .MatchStartForward(
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(EClass), nameof(EClass.player))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Player), nameof(Player.TryEquipBait))))
            .RemoveInstructions(2)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_2),
                Transpilers.EmitDelegate((AI_Fish thiz) => {
                    if (NetSession.Instance.IsClient) {
                        EClass.player.TryEquipBait();
                        return;
                    }

                    if (thiz.owner.IsPC) {
                        EClass.player.TryEquipBait();
                        return;
                    }
                }))
            // if (EClass.player.eqBait == null)
            .MatchStartForward(
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(EClass), nameof(EClass.player))))
            .RemoveInstructions(2)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_2),
                Transpilers.EmitDelegate((AI_Fish thiz) => !thiz.owner.IsPC || EClass.player.eqBait is not null))
            // if (this.owner == EClass.pc)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldloc_2),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(EClass), nameof(EClass.pc))))
            .SetInstructionAndAdvance(
                Transpilers.EmitDelegate((Chara chara) => chara.IsPlayer))
            .SetOpcodeAndAdvance(OpCodes.Brfalse_S)
            .InstructionEnumeration();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AI_Fish), nameof(AI_Fish.Makefish))]
    internal static bool OnMakefish(ref Thing? __result)
    {
        if (CharaProgressCompleteDelta.Current is not { } delta) {
            return true;
        }

        __result = delta.TryGetProduct();
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AI_Fish), nameof(AI_Fish.Makefish))]
    internal static void OnMakefishEnd(Chara c, Thing? __result)
    {
        if (!CharaProgressCompleteEvent.IsHappening || NetSession.Instance.IsClient) {
            return;
        }

        CharaProgressCompleteEvent.DeltaList.Add(new ThingDelta {
            Thing = __result,
        });

        if (c.IsRemotePlayer) {
            var bait = c.things.Find(t => t.trait is TraitBait tb && tb.EQ == t);
            bait.ModNum(-1);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AI_Fish.ProgressFish), nameof(AI_Fish.ProgressFish.OnProgressComplete))]
    internal static void OnProgressComplete(AI_Fish.ProgressFish __instance)
    {
        if (CharaProgressCompleteDelta.Current is not { } delta) {
            return;
        }

        if (delta.DeltaList.Find(d => d is ThingDelta) is null) {
            __instance.hit = 0;
        } else {
            __instance.hit = 100;
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AI_Fish.ProgressFish), nameof(AI_Fish.ProgressFish.OnProgress))]
    internal static IEnumerable<CodeInstruction> OnProgress(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            // this.progress = this.MaxProgress;
            .MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(AIAct), nameof(AIAct.MaxProgress))),
                new CodeMatch(OpCodes.Stfld))
            .RemoveInstructions(3)
            .InsertAndAdvance(
                Transpilers.EmitDelegate((AI_Fish.ProgressFish thiz) => {
                    if (NetSession.Instance.Connection is ElinNetClient) {
                        thiz.hit = 1;
                    } else {
                        thiz.progress = thiz.MaxProgress;
                    }
                }))
            .MatchStartForward(
                new CodeMatch(OpCodes.Ret))
            .Advance(2)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((AI_Fish.ProgressFish thiz) => {
                    if (NetSession.Instance.Connection is not ElinNetClient) {
                        return;
                    }

                    var progress = thiz.progress + int.MaxValue - 1;
                    if (progress == 2 || (progress >= 8 && progress % 6 == 0 && EClass.rnd(3) == 0)) {
                        thiz.owner.renderer.PlayAnime(AnimeID.Shiver);
                        thiz.Ripple();
                    }
                }))
            // this.hit = 0
            .MatchStartForward(
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(AI_Fish.ProgressFish), nameof(AI_Fish.ProgressFish.hit))))
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((AI_Fish.ProgressFish thiz) => {
                    if (NetSession.Instance.Connection is ElinNetHost host) {
                        host.Delta.AddRemote(new CharaHitFishDelta { Owner = thiz.owner });
                    }
                }))
            .InstructionEnumeration();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TraitBait), nameof(TraitBait.EQ), MethodType.Getter)]
    internal static bool OnGetEQ(TraitBait __instance, ref Thing __result)
    {
        if (__instance.owner.GetRootCard()?.IsPC is not false) {
            return true;
        }

        var field = AccessTools.Field(__instance.GetType(), "<EQ>k__BackingField");
        __result = (Thing)field.GetValue(__instance);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TraitBait), nameof(TraitBait.EQ), MethodType.Setter)]
    internal static bool OnSetEQ(TraitBait __instance, Thing value)
    {
        if (__instance.owner.GetRootCard() is not Chara { IsPC: false } owner) {
            return true;
        }

        owner.things
            .Where(t => t.trait is TraitBait tb && tb.EQ == t && tb != __instance)
            .Select(t => t.trait as TraitBait)
            .ForEach(tb => {
                tb!.EQ = null;
            });

        var field = AccessTools.Field(__instance.GetType(), "<EQ>k__BackingField");
        field.SetValue(__instance, value);

        return false;
    }
}