using System;
using System.Collections.Generic;
using System.Reflection;
using Cwl.API.Attributes;
using Cwl.Helper.Runtime;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Sources;

[HarmonyPatch]
internal class CardOnCreateEvent
{
    private static event Action<Chara> OnCharaCreateEvent = delegate { };

    private static event Action<Thing> OnThingCreateEvent = delegate { };

    public static void Add(Action<Chara> process)
    {
        OnCharaCreateEvent += SafeInvoke(process, (chara, ex) =>
            "cwl_warn_processor".Loc("chara_on_create", chara.id, ex));
    }

    public static void Add(Action<Thing> process)
    {
        OnThingCreateEvent += SafeInvoke(process, (thing, ex) =>
            "cwl_warn_processor".Loc("thing_on_create", thing.id, ex));
    }

    private static Action<T> SafeInvoke<T>(Action<T> process, Func<T, Exception, string> exceptionLogger)
    {
        return target => {
            try {
                process(target);
            } catch (Exception ex) {
                CwlMod.Warn<CardOnCreateEvent>(exceptionLogger(target, ex));
                // noexcept
            }
        };
    }

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return [
            ..OverrideMethodComparer.FindAllOverrides(typeof(Card), "OnDeserialized"),
            AccessTools.Method(typeof(Card), nameof(Card.Create)),
        ];
    }

    [HarmonyPostfix]
    internal static void OnCreate(Card __instance)
    {
        switch (__instance) {
            case Chara chara:
                OnCharaCreateEvent(chara);
                break;
            case Thing thing:
                OnThingCreateEvent(thing);
                break;
        }
    }

    [Time]
    internal static void RegisterEvents(MethodInfo method, CwlOnCreateEvent onCreate)
    {
        var type = "";
        switch (onCreate) {
            case CwlCharaOnCreateEvent:
                Add((Chara chara) => method.FastInvokeStatic(chara));
                type = "chara";
                break;
            case CwlThingOnCreateEvent:
                Add((Thing thing) => method.FastInvokeStatic(thing));
                type = "thing";
                break;
        }

        CwlMod.Log<CardOnCreateEvent>("cwl_log_processor_add".Loc(type, "on_create", method.GetAssemblyDetail(false)));
    }
}