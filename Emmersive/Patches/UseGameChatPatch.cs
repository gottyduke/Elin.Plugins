using System;
using System.Collections.Generic;
using System.Reflection;
using Emmersive.Components;
using Emmersive.Helper;
using EModding.Helper;
using HarmonyLib;

namespace Emmersive.Patches;

[HarmonyPatch]
internal class UseGameChatPatch
{
    private static readonly MethodInfo? _inputName = AccessTools.Method(
        typeof(Dialog),
        nameof(Dialog.InputName),
        [typeof(string), typeof(string), typeof(Action<bool, string>), typeof(Dialog.InputType)]);

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AM_Adv), nameof(AM_Adv._OnUpdateInput))]
    internal static IEnumerable<CodeInstruction> OnCallBasicInputIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(ci => ci.Calls(_inputName)))
            .EnsureValid("replace Dialog.InputName")
            .SetInstruction(
                Transpilers.EmitDelegate(UseEmmersiveChat))
            .InstructionEnumeration();
    }

    private static Dialog UseEmmersiveChat(string langDetail, string text, Action<bool, string> onClose, Dialog.InputType type)
    {
        return EmTalkTrigger.ShowPlayerTalkDialog();
    }
}