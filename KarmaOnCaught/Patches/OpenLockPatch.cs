using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace KoC.Patches;

[HarmonyPatch]
internal class OpenLockPatch
{
    internal static bool Prepare()
    {
        return KocConfig.PatchLockpick?.Value ?? false;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Trait), nameof(Trait.TryOpenLock))]
    internal static IEnumerable<CodeInstruction> OnUnlockSuccessIl(IEnumerable<CodeInstruction> instructions)
    {
        var owner = AccessTools.Field(
            typeof(Trait),
            nameof(Trait.owner));

        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, owner),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(
                    typeof(Card),
                    nameof(Card.isLostProperty))),
                new CodeMatch(OpCodes.Brfalse))
            .RemoveInstructions(2)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, owner),
                new CodeInstruction(OpCodes.Ldloc_2))
            .Advance(1)
            .SetInstructionAndAdvance(
                Transpilers.EmitDelegate(TryWitnessOpenLock))
            .InstructionEnumeration();
    }

    private static void TryWitnessOpenLock(bool isCrime, Chara cc, Card owner, int lockLv, int modifier)
    {
        if (!cc.IsPC) {
            return;
        }

        var detection = KocConfig.DetectionRadius!.Value;
        var witnesses = owner.pos.ListWitnesses(cc, detection);
        if (witnesses.Count == 0) {
            return;
        }

        var swiftUnlock = cc.Evalue("lockpicking") + cc.DEX / 2;
        var baseCost = lockLv / witnesses.Count;
        var totalCost = witnesses.Sum(w => {
            var los = w.CanSee(cc) ? 0.5f : 0f;
            var perception = w.PER / (2f - los);
            return EClass.rnd((int)perception + baseCost);
        });

        var suspicion = (float)totalCost / swiftUnlock;
        KocMod.DoModKarma(isCrime && swiftUnlock < totalCost, cc, modifier, suspicion >= 0.9f, witnesses.Count);
    }
}