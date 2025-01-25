﻿using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API.Custom;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class RerouteDramaPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Chara), nameof(Chara.ShowDialog), [])]
    internal static IEnumerable<CodeInstruction> OnSwitchIdIl(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchEndForward(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(
                    typeof(Card),
                    nameof(Card.id))),
                new(OpCodes.Stloc_S),
                new(OpCodes.Ldloc_S))
            .CreateLabel(out var label)
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(TryRerouteDialog),
                new(OpCodes.Brfalse, label),
                new(OpCodes.Ret))
            .InstructionEnumeration();
    }

    // TODO: this only reroutes if it's actually going to show
    // subject to change for dialog expansion
    private static bool TryRerouteDialog(Chara chara)
    {
        if (!CustomChara.DramaRoutes.TryGetValue(chara.id, out var drama)) {
            return false;
        }

        try {
            chara.ShowDialog(drama);
        } catch (Exception ex) {
            CwlMod.Error<CustomChara>("cwl_error_failure".Loc(ex));
            if (ELayer.ui.TopLayer is LayerDrama layer) {
                layer.Close();
            }
            // noexcept
        }

        return true;
    }
}