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
        var acts = typeof(Act).Assembly.GetTypes()
            .OfDerived(typeof(Act))
            .Concat(TypeQualifier.Declared.OfDerived(typeof(Act)));

        HashSet<string> cached = [];
        foreach (var act in acts) {
            if (act == typeof(DynamicAct) || act == typeof(DynamicAIAct)) {
                continue;
            }

            var method = act.GetRuntimeMethod(nameof(Act.Perform), []);
            if (method is null) {
                continue;
            }

            if (!cached.Add($"{method.DeclaringType!.FullName}/{method.FullDescription()}")) {
                continue;
            }

            yield return method;
        }
    }

    [HarmonyPostfix]
    internal static void OnPerform(Act __instance)
    {
        OnActPerformEvent(__instance);
    }
}