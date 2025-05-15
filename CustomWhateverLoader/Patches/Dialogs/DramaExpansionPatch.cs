using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.API.Drama;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class DramaExpansionPatch
{
    internal static bool Prepare()
    {
        return CwlConfig.ExpandedActions;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DramaManager), nameof(DramaManager.ParseLine))]
    internal static IEnumerable<CodeInstruction> OnParseActionIl(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchEndForward(
                new OperandContains(OpCodes.Ldfld, "action"),
                new(OpCodes.Stloc_S))
            .CreateLabel(out var label)
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_1),
                Transpilers.EmitDelegate(ExternalInvoke),
                new(OpCodes.Brfalse, label),
                new(OpCodes.Pop),
                new(OpCodes.Ret))
            .InstructionEnumeration();
    }

    [SwallowExceptions]
    private static bool ExternalInvoke(DramaManager __instance, Dictionary<string, string> item)
    {
        if (!item.TryGetValue("action", out var action) || action is not ("invoke*" or "inject")) {
            return false;
        }

        if (!item.TryGetValue("param", out var rawExpr)) {
            return false;
        }

        if (action == "inject") {
            if (rawExpr == "Unique") {
                InjectUniqueRumor(__instance);
            }

            return false;
        }

        // default actor
        item.TryAdd("actor", "tg");

        foreach (var expr in rawExpr.SplitLines()) {
            var func = DramaExpansion.BuildExpression(expr);
            if (func is null) {
                continue;
            }

            var step = new DramaEventMethod(() => func(__instance, item));
            if (item.TryGetValue("jump", out var jump) && !jump.IsEmpty()) {
                step.action = null;
                step.jumpFunc = () => func(__instance, item) ? jump : "";
            }

            __instance.AddEvent(step);
        }

        return true;
    }

    [SwallowExceptions]
    private static void InjectUniqueRumor(DramaManager dm)
    {
        var chara = dm.tg.chara;
        var rumors = Lang.GetDialog("unique", chara.id);
        if (rumors.Length == 1 && rumors[0] == chara.id) {
            return;
        }

        var rumor = GetUniqueRumor(chara, dm.enableTone);

        dm.CustomEvent(dm.sequence.Exit);

        var choice = new DramaChoice("letsTalk".lang(), dm.setup.step);
        dm.lastTalk.AddChoice(choice);
        dm._choices.Add(choice);

        choice.SetOnClick(() => {
            var firstText = rumor;
            dm.sequence.firstTalk.funcText = () => firstText;
            rumor = GetUniqueRumor(chara, dm.enableTone);
            chara.affinity.OnTalkRumor();
            choice.forceHighlight = true;
        });
    }

    private static string GetUniqueRumor(Chara chara, bool tone = false)
    {
        var dialog = Lang.GetDialog("unique", chara.id).RandomItem();
        return tone ? chara.ApplyTone(dialog) : dialog;
    }
}