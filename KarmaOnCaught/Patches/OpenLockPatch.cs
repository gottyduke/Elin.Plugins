using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace KoC.Patches;

[HarmonyPatch]
internal class OpenLockPatch
{
    private static bool _applied;

    private static KocConfig.Patch Config => KocConfig.Managed["Lockpick"];

    internal static bool Prepare()
    {
        return Config.Enabled!.Value;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Trait), nameof(Trait.TryOpenLock))]
    internal static IEnumerable<CodeInstruction> OnUnlockSuccessIl(IEnumerable<CodeInstruction> instructions)
    {
        if (_applied) {
            return instructions;
        }

        _applied = true;

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
        if (!isCrime || !cc.IsPC) {
            return;
        }

        var difficulty = 0f;
        var detection = Config.DetectionRadius!.Value;
        var mod = Config.DifficultyModifier!.Value;
        var skill = (cc.Evalue("lockpicking") + cc.DEX) / 2f;

        var witnesses = owner.pos.ListWitnesses(cc, detection).Count;
        var caught = owner.pos.TryWitnessCrime(cc, radius: detection, funcWitness: w => {
            var los = w.CanSee(cc) ? 50 : 0;
            var perception = w.PER * (75 + los) / 100;

            var randomCost = EClass.rndf(perception + lockLv + mod);
            difficulty += randomCost;

            return randomCost > skill;
        });

        var suspicion = difficulty / skill;
        KocMod.DoModKarma(caught, cc, modifier, suspicion >= 0.65f, witnesses);
    }
}