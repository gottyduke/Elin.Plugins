using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace KoC.Patches;

[HarmonyPatch]
internal class TaskDwarfPatch
{
    private static bool PatchEnabled => KocConfig.PatchDwarf?.Value ?? false;

    [HarmonyPatch]
    internal class OnCrimeWitnessSubPatch
    {
        private const string OnCrimeWitnessClosure = $"<{nameof(TaskDig.OnCreateProgress)}>b__1";

        private static Type? _closureDig;
        private static Type? _closureMine;

        internal static bool Prepare()
        {
            _closureDig = AccessTools.FirstInner(typeof(TaskDig), t => t.Name.Contains("DisplayClass18_0"));
            _closureMine = AccessTools.FirstInner(typeof(TaskMine), t => t.Name.Contains("DisplayClass22_0"));
            return PatchEnabled && _closureDig is not null && _closureMine is not null;
        }

        internal static IEnumerable<MethodInfo> TargetMethods()
        {
            return [
                AccessTools.Method(_closureDig, OnCrimeWitnessClosure),
                AccessTools.Method(_closureMine, OnCrimeWitnessClosure),
            ];
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
                    Transpilers.EmitDelegate(TryWitnessDwarf))
                .InstructionEnumeration();
        }

        private static bool TryWitnessDwarf(Point pos, Chara cc, Chara? target, int radius, Func<Chara, bool>? func)
        {
            var difficulty = 0;
            var detection = KocConfig.DetectionRadius!.Value;
            var witnesses = pos.ListWitnesses(cc, detection).Count;

            var skill = (cc.Evalue("mining") + cc.DEX) / 2;
            var caught = pos.TryWitnessCrime(cc, radius: detection, funcWitness: w => {
                var los = w.CanSee(cc) ? 0.5f : 0f;
                var perception = w.PER / (2f - los);

                var randomCost = EClass.rnd((int)perception);
                difficulty += randomCost;

                return randomCost > skill;
            });

            var suspicion = (float)difficulty / (cc.DEX * witnesses);
            KocMod.DoModKarma(caught, cc, -1, suspicion >= 0.9f, witnesses);
            return caught;
        }
    }

    [HarmonyPatch]
    internal class OnProgressCompleteSubPatch
    {
        internal static bool Prepare()
        {
            return PatchEnabled;
        }

        internal static IEnumerable<MethodInfo> TargetMethods()
        {
            return [
                AccessTools.Method(typeof(TaskDig), nameof(TaskDig.OnProgressComplete)),
                AccessTools.Method(typeof(TaskMine), nameof(TaskMine.OnProgressComplete)),
            ];
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