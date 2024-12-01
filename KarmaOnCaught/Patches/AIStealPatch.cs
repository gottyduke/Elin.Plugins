using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace KoC.Patches;

[HarmonyPatch]
internal class AIStealPatch
{
    private static Type? _closures;

    private static bool PatchEnabled => (KocConfig.PatchSteal?.Value ?? false) && _closures is not null;

    [HarmonyPrepare]
    internal static void DefineType()
    {
        _closures = AccessTools.FirstInner(typeof(AI_Steal), t => t.Name.Contains("DisplayClass9_0"));
    }

    [HarmonyPatch]
    internal class OnCrimeWitnessSubPatch
    {
        private const string OnCrimeWitnessClosure = $"<{nameof(AI_Steal.Run)}>b__2";

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
                .SetAndAdvance(OpCodes.Call, AccessTools.Method(
                    typeof(OnCrimeWitnessSubPatch),
                    nameof(TryWitnessPickpocket)))
                .InstructionEnumeration();
        }

        private static bool TryWitnessPickpocket(Point pos, Chara cc, Chara? target, int radius, Func<Chara, bool> func)
        {
            var detection = KocConfig.DetectionRadius!.Value;
            var caught = pos.TryWitnessCrime(cc, target, detection, func);
            var witnesses = pos.ListWitnesses(cc, detection).Count;

            KocMod.DoModKarma(caught, cc, -1, false, witnesses);
            return caught;
        }
    }

    [HarmonyPatch]
    internal class OnProgressCompleteSubPatch
    {
        private const string OnProgressCompleteClosure = $"<{nameof(AI_Steal.Run)}>b__3";

        internal static bool Prepare()
        {
            return PatchEnabled;
        }

        internal static MethodInfo TargetMethod()
        {
            return AccessTools.Method(_closures, OnProgressCompleteClosure);
        }

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> OnProgressCompleteIl(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            return new CodeMatcher(instructions, generator)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldc_I4_0),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.PropertySetter(
                        typeof(Card),
                        nameof(Card.isNPCProperty))))
                .CreateLabel(out var jmp)
                .Start()
                .MatchEndForward(
                    new CodeMatch(OpCodes.Ldnull),
                    new CodeMatch(OpCodes.Ldnull),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(
                        typeof(Card),
                        nameof(Card.Say),
                        [typeof(string), typeof(Card), typeof(Card), typeof(string), typeof(string)])))
                .Advance(1)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Br, jmp))
                .InstructionEnumeration();
        }
    }
}