using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Emmersive.Components;
using Emmersive.Helper;
using HarmonyLib;

namespace Emmersive.Patches;

[HarmonyPatch]
internal class UseGameChatPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AM_Adv), nameof(AM_Adv._OnUpdateInput))]
    internal static IEnumerable<CodeInstruction> OnCallBasicInputIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new OperandContains(OpCodes.Call, nameof(Dialog.InputName)))
            .SetInstruction(
                Transpilers.EmitDelegate(UseEmmersiveChat))
            .InstructionEnumeration();
    }

    private static Dialog UseEmmersiveChat(string langDetail, string text, Action<bool, string> onClose, Dialog.InputType type)
    {
        return EmTalkTrigger.ShowPlayerTalkDialog();
    }
}