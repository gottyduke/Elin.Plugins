using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace KoC.Patches;

internal class OnModKarmaPatch
{
    internal static readonly HashSet<MethodInfo> ToRemove = [];

    internal static bool Prepare()
    {
        return ToRemove.Count > 0;
    }

    internal static IEnumerable<MethodInfo> TargetMethods()
    {
        return ToRemove;
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnModKarmaIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(
                    typeof(Player),
                    nameof(Player.ModKarma),
                    [typeof(int)])))
            .Repeat(cm => cm
                .RemoveInstruction()
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Pop)))
            .InstructionEnumeration();
    }
}