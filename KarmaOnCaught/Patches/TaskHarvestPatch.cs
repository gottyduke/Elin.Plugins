using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace KoC.Patches;

[HarmonyPatch]
internal class TaskHarvestPatch
{
    private static Type? _closures;

    private static bool PatchEnabled => (KocConfig.PatchHarvest?.Value ?? false) && _closures is not null;

    [HarmonyPrepare]
    internal static void DefineType()
    {
        _closures = AccessTools.FirstInner(typeof(TaskHarvest), t => t.Name.Contains("DisplayClass23_0"));
    }

    [HarmonyPatch]
    internal class OnCrimeWitnessSubPatch
    {
        private const string OnCrimeWitnessClosure = $"<{nameof(TaskHarvest.OnCreateProgress)}>b__1";

        internal static bool Prepare()
        {
            return PatchEnabled;
        }

        internal static MethodInfo TargetMethod()
        {
            return AccessTools.Method(_closures, OnCrimeWitnessClosure);
        }

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> OnCrimeWitnessIl(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .End()
                .MatchEndBackwards(
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(
                        typeof(Point),
                        nameof(Point.TryWitnessCrime))))
                .SetInstruction(
                    Transpilers.EmitDelegate(TryWitnessHarvest))
                .InstructionEnumeration();
        }

        private static bool TryWitnessHarvest(Point pos, Chara cc, Chara? target, int radius, Func<Chara, bool>? func)
        {
            var difficulty = 0;
            var detection = KocConfig.DetectionRadius!.Value;
            var witnesses = pos.ListWitnesses(cc, detection).Count;

            var caught = pos.TryWitnessCrime(cc, radius: detection, funcWitness: w => {
                var los = w.CanSee(cc) ? 0.5f : 0f;
                var perception = w.PER / (2f - los);

                var randomCost = EClass.rnd((int)perception);
                difficulty += randomCost;

                return randomCost > cc.DEX;
            });

            var suspicion = (float)difficulty / (cc.DEX * witnesses);
            KocMod.DoModKarma(caught, cc, -1, suspicion >= 0.9f, witnesses);
            return caught;
        }
    }

    [HarmonyPatch]
    internal class OnProgressCompleteSubPatch
    {
        private const string OnProgressCompleteClosure = $"<{nameof(TaskHarvest.OnCreateProgress)}>b__2";

        internal static bool Prepare()
        {
            return PatchEnabled;
        }

        internal static MethodInfo TargetMethod()
        {
            return AccessTools.Method(_closures, OnProgressCompleteClosure);
        }

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> OnProgressCompleteIl(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchEndForward(
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(
                        typeof(Zone),
                        nameof(Zone.IsCrime))),
                    new CodeMatch(OpCodes.Brfalse))
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Pop))
                .SetOpcodeAndAdvance(OpCodes.Br)
                .InstructionEnumeration();
        }
    }
}