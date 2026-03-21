using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class InvSplitThingEvent
{

    internal static MethodInfo TargetMethod()
    {
        return AccessTools.Method(
            AccessTools.FirstInner(typeof(Thing), t => t.Name.Contains("DisplayClass45_0")), "<ShowSplitMenu>g__Process|1");
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnProcess(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Sub))
            .RemoveInstructions(3)
            .Advance(2)
            .RemoveInstructions(1)
            .MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0))
            .RemoveInstructions(34)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldloc_1),
                Transpilers.EmitDelegate((DragItemCard dragItemCard, Thing t) => {
                    if (NetSession.Instance.IsHost) {
                        dragItemCard.from.thing = t;
                        EClass.ui.StartDrag(dragItemCard);
                        return;
                    }

                    ThingRequest
                        .Create(dragItemCard.from.thing, t.Num)
                        .Then(thing => {
                            dragItemCard.from.thing = thing;
                            EClass.ui.StartDrag(dragItemCard);
                        });
                }))
            .Advance(1)
            .RemoveInstructions(2)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Pop))
            .InstructionEnumeration();

    }
}