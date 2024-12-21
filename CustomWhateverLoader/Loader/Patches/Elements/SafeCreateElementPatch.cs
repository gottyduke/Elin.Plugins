using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API;
using Cwl.API.Custom;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Loader.Patches.Elements;

[HarmonyPatch]
internal class SafeCreateElementPatch
{
    internal static bool Prepare()
    {
        return CwlConfig.SafeCreateClass;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Element), nameof(Element.Create), typeof(int), typeof(int))]
    internal static IEnumerable<CodeInstruction> OnCreateIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldstr, nameof(Element)),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Ldstr, "Elin"),
                new CodeMatch(o => o.opcode == OpCodes.Call &&
                                   o.operand.ToString().Contains(nameof(ClassCache.Create))))
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(SafeCreateInvoke))
            .RemoveInstruction()
            .InstructionEnumeration();
    }

    [Time]
    private static Element SafeCreateInvoke(string unqualified, string assembly, int id)
    {
        try {
            var element = ClassCache.Create<Element>(unqualified, assembly);
            if (element is not null) {
                return element;
            }

            throw new SourceParseException("cwl_warn_deserialize_ele");
        } catch (Exception ex) {
            CwlMod.Warn(ex.Message.Loc(id, unqualified, CwlConfig.Patches.SafeCreateClass!.Definition.Key));
            // noexcept
        }

        var row = EMono.sources.elements.map.TryGetValue(id)!;
        row.name = "cwl_ele_safety_cone".Loc(id, row.alias, unqualified);
        row.detail = "cwl_ele_safety_desc".Loc();

        return new CustomElement();
    }
}