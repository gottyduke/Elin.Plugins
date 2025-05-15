using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.API.Drama;
using Cwl.Helper.Extensions;
using Cwl.Helper.Runtime;
using HarmonyLib;

namespace Cwl.Patches.Dialogs;

[HarmonyPatch]
internal class DynamicDramaEvaluation
{
    internal static readonly Dictionary<DramaEvent, Func<bool>> ActiveConditions = [];

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DramaManager), nameof(DramaManager.ParseLine))]
    internal static void OnParseLineItem(DramaManager __instance, Dictionary<string, string> item)
    {
        //! cookie must be set first to share parse state between patches
        DramaExpansion.Cookie = new(__instance, item);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DramaSequence), nameof(DramaSequence.Exit))]
    internal static void OnSequenceExit()
    {
        ActiveConditions.Clear();
    }

    [HarmonyPatch]
    internal class ChoiceConditionPatch
    {
        internal static bool Prepare()
        {
            return CwlConfig.DynamicCheckIf;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DramaManager), nameof(DramaManager.ParseLine))]
        internal static IEnumerable<CodeInstruction> OnPreCheckIfIl(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            return new CodeMatcher(instructions)
                // make all CheckIF() true on load
                .MatchStartForward(
                    new OperandContains(OpCodes.Call, nameof(DramaManager.CheckIF)))
                .Repeat(cm => cm
                    .SetInstructionAndAdvance(
                        Transpilers.EmitDelegate(EnableAllIf)))
                .Start()
                // attach all if(n) to drama choice
                .MatchStartForward(
                    new OperandContains(OpCodes.Callvirt, nameof(DramaEventTalk.AddChoice)))
                .Repeat(cm => cm
                    .InsertAndAdvance(
                        Transpilers.EmitDelegate(AttachActiveCondition))
                    .RemoveInstruction())
                .InstructionEnumeration();
        }

        private static bool EnableAllIf(DramaManager dm, string condition)
        {
            return true;
        }

        private static DramaChoice AttachActiveCondition(DramaEventTalk talk, DramaChoice choice)
        {
            // can't use ActiveConditions because lastTalk is re-used
            var ifs = ActiveConditions[talk] = () => (DramaExpansion.Cookie?.Line ?? [])
                .Where(kv => kv.Key.StartsWith("if") && !kv.Value.IsEmpty())
                .Select(kv => kv.Value)
                .All(talk.manager.CheckIF);

            var prev = choice.activeCondition;
            choice.IF = string.Empty;

            return choice.SetCondition(() => ifs() && (prev?.Invoke() ?? true));
        }
    }

    [HarmonyPatch]
    internal class DramaEventAttachIfPatch
    {
        internal static bool Prepare()
        {
            return CwlConfig.DynamicCheckIf;
        }

        internal static IEnumerable<MethodBase> TargetMethods()
        {
            return OverrideMethodComparer.FindAllOverridesCtor(typeof(DramaEvent));
        }

        [HarmonyPostfix]
        internal static void OnInstantiateDramaEvent(DramaEvent __instance)
        {
            if (DramaExpansion.Cookie is not { Dm: { } dm, Line: { } item }) {
                return;
            }

            // allow multiple if(n) where n > 2
            ActiveConditions[__instance] = () => item
                .Where(kv => kv.Key.StartsWith("if") && !kv.Value.IsEmpty())
                .Select(kv => kv.Value)
                .All(dm.CheckIF);
        }
    }

    [HarmonyPatch]
    internal class DramaEventPlayPatch
    {
        internal static bool Prepare()
        {
            return CwlConfig.DynamicCheckIf;
        }

        internal static IEnumerable<MethodBase> TargetMethods()
        {
            return OverrideMethodComparer.FindAllOverrides(typeof(DramaEvent), nameof(DramaEvent.Play));
        }

        [HarmonyPrefix]
        internal static bool ExternalCheckIfOnPlay(DramaEvent __instance, ref bool __result)
        {
            return !ActiveConditions.TryGetValue(__instance, out var condition) || condition();
        }
    }
}