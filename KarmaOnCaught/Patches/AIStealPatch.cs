using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace KoC.Patches;

[HarmonyPatch]
internal class AIStealPatch
{
    private const string OnProgressCompleteClosure = $"<{nameof(AI_Steal.Run)}>b__3";
    private const string OnCrimeWitnessClosure = $"<{nameof(AI_Steal.Run)}>b__4";
    private static Type? _closures;
    
    private static bool PatchEnabled => (KocConfig.PatchSteal?.Value ?? false) && _closures is not null;

    [HarmonyPrepare]
    internal static void DefineType()
    {
        _closures = AccessTools.FirstInner(typeof(AI_Steal), t => t.Name.Contains("DisplayClass9_0"));
    }
    
    [HarmonyPatch]
    internal class OnProgressCompleteSubPatch
    {
        internal static bool Prepare() => PatchEnabled;
        
        internal static MethodInfo TargetMethod() => AccessTools.Method(_closures, OnProgressCompleteClosure);

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
    
    [HarmonyPatch]
    internal class OnCrimeWitnessSubPatch
    {
        internal static bool Prepare() => PatchEnabled;
        
        internal static MethodInfo TargetMethod() => AccessTools.Method(_closures, OnCrimeWitnessClosure);

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> OnCrimeWitnessIl(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchEndForward(
                    new CodeMatch(OpCodes.Ldloc_0),
                    new CodeMatch(OpCodes.Add),
                    new CodeMatch(OpCodes.Cgt),
                    new CodeMatch(OpCodes.Ret))
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Dup),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(_closures, "chara")),
                    new CodeInstruction(OpCodes.Ldc_I4_M1),
                    Transpilers.EmitDelegate(KocMod.DoModKarma))
                .InstructionEnumeration();
        }
    }
}