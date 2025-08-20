using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Cwl.API.Custom;
using Cwl.Helper;
using Cwl.Helper.Extensions;
using HarmonyLib;

namespace Cwl.Patches.Elements;

[HarmonyPatch(typeof(Feat), nameof(Feat.Apply))]
internal class FeatApplyEvent
{
    private static string[]? _aliases;

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnGodHintIl(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchStartForward(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldloca_S),
                new OperandContains(OpCodes.Call, "GodHint"))
            .EnsureValid("load GodHint")
            .CreateLabel(out var label)
            .Start()
            .MatchEndForward(
                new(OpCodes.Ldarg_0),
                new OperandContains(OpCodes.Ldfld, nameof(Element.id)),
                new OpCodeContains(nameof(OpCodes.Stloc)))
            .EnsureValid("switch table")
            .Advance(1)
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(IsExternalGodHint),
                new(OpCodes.Brtrue, label))
            .InstructionEnumeration();
    }

    [SwallowExceptions]
    [HarmonyPostfix]
    internal static void OnApply(Feat __instance, int a, ElementContainer owner, bool hint)
    {
        if (!CustomElement.Managed.ContainsKey(__instance.id)) {
            return;
        }

        __instance.InstanceDispatch("_OnApply", a, owner, hint);
    }

    private static bool IsExternalGodHint(Feat feat)
    {
        _aliases ??= CustomReligion.Managed.Values.Select(r => r.FeatGodAlias).ToArray();
        return _aliases.Contains(feat.source?.alias);
    }
}