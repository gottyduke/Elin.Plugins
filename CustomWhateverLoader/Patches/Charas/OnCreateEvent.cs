using System.Collections.Generic;
using System.Reflection;
using Cwl.API.Attributes;
using Cwl.Helper.Runtime;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Charas;

internal class CharaOnCreateEvent
{
    public delegate void OnCharaCreate(Chara chara);

    private static bool _applied;

    private static event OnCharaCreate OnCharaCreateEvent = delegate { };

    public static void Add(OnCharaCreate process)
    {
        if (!_applied) {
            //Harmony.CreateAndPatchAll(typeof(CharaOnCreateEvent), ModInfo.Guid);
        }

        _applied = true;

        OnCharaCreateEvent += Process;
        return;

        void Process(Chara chara)
        {
            try {
                process(chara);
            } catch {
                // noexcept
            }
        }
    }

    internal static IEnumerable<MethodInfo> TargetMethods()
    {
        return [
            AccessTools.Method(typeof(Chara), "OnDeserialized"),
            AccessTools.Method(typeof(Chara), nameof(Chara.OnCreate)),
        ];
    }

    [HarmonyPostfix]
    internal static void OnCreate(Chara __instance)
    {
        OnCharaCreateEvent(__instance);
    }

    [Time]
    internal static void RegisterEvents(MethodInfo method, CwlCharaOnCreateEvent onCreate)
    {
        Add(ctx => method.FastInvokeStatic(ctx));

        CwlMod.Log<CharaOnCreateEvent>("cwl_log_processor_add".Loc("chara", "creation", method.GetAssemblyDetail(false)));
    }
}