using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.API.Attributes;
using Cwl.API.Processors;
using Cwl.Helper;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Elements;

public class ActPerformEvent
{
    private static event Action<Act>? OnActPerformEvent;

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(Act), nameof(Act.Perform))
            .Where(mi => mi.DeclaringType != typeof(DynamicAct) && mi.DeclaringType != typeof(DynamicAIAct));
    }

    [HarmonyPostfix]
    internal static void OnPerform(Act __instance)
    {
        OnActPerformEvent?.Invoke(__instance);
    }

    [Time]
    internal static void RegisterEvents(MethodInfo method, CwlActPerformEvent attr)
    {
        Add(act => method.FastInvokeStatic(act));

        CwlMod.Log<GameIOProcessor.GameIOContext>("cwl_log_processor_add".Loc("act", "perform", method.GetAssemblyDetail(false)));
    }

    public static void Add(Action<Act> process)
    {
        OnActPerformEvent += Process;
        return;

        void Process(Act act)
        {
            try {
                process(act);
            } catch (Exception ex) {
                CwlMod.Warn<ActPerformEvent>("cwl_warn_processor".Loc("act_perform", act.GetType().Name, ex));
                // noexcept
            }
        }
    }
}