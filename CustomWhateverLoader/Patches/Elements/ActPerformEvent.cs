using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.API.Attributes;
using Cwl.API.Processors;
using Cwl.Helper;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Elements;

public class ActPerformEvent
{
    private static bool _patched;
    private static event Action<Act>? OnActPerformEvent;

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(Act), nameof(Act.Perform))
            .Where(mi => mi.DeclaringType != typeof(DynamicAct) && mi.DeclaringType != typeof(DynamicAIAct));
    }

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

    private static void Add(Action<Act> process)
    {
        if (!_patched) {
            CoroutineHelper.Deferred(TryPatch);
            _patched = true;
        }

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

    [SwallowExceptions]
    private static void TryPatch()
    {
        var postfix = new HarmonyMethod(typeof(ActPerformEvent), nameof(OnPerform));
        foreach (var perform in TargetMethods()) {
            try {
                CwlMod.SharedHarmony.Patch(perform, postfix: postfix);
            } catch {
                CwlMod.Warn<ActPerformEvent>("cwl_warn_processor".Loc("act_perform",
                    (perform as MethodInfo)?.GetAssemblyDetail(false), null));
                // noexcept
            }
        }
    }
}