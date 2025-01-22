using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.Helper.Runtime;
using HarmonyLib;

namespace Cwl.Patches.Elements;

public class ActPerformEvent
{
    public delegate void OnActPerform(Act act);

    private static bool _applied;

    private static event OnActPerform OnActPerformEvent = delegate { };

    public static void Add(OnActPerform process)
    {
        if (!_applied) {
            Harmony.CreateAndPatchAll(typeof(ActPerformEvent), ModInfo.Guid);
        }

        _applied = true;

        OnActPerformEvent += Process;
        return;

        void Process(Act act)
        {
            try {
                process(act);
            } catch {
                // noexcept
            }
        }
    }

    internal static IEnumerable<MethodInfo> TargetMethods()
    {
        return OverrideMethodComparer.FindAllOverrides(typeof(Act), nameof(Act.Perform))
            .Where(mi => mi.DeclaringType != typeof(DynamicAct) && mi.DeclaringType != typeof(DynamicAIAct));
    }

    [HarmonyPostfix]
    internal static void OnPerform(Act __instance)
    {
        OnActPerformEvent(__instance);
    }
}