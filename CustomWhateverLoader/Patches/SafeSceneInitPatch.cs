using System;
using System.Collections.Generic;
using Cwl.API.Custom;
using Cwl.Helper.Unity;
using HarmonyLib;

namespace Cwl.Patches;

[HarmonyPatch(typeof(Scene), nameof(Scene.Init))]
internal class SafeSceneInitPatch
{
    internal static bool SafeToCreate;
    internal static readonly Queue<Action> Cleanups = [];

    [HarmonyPrefix]
    internal static void PostCleanup(Scene.Mode newMode)
    {
        if (newMode != Scene.Mode.StartGame) {
            return;
        }

        while (Cleanups.TryDequeue(out var cleanup)) {
            try {
                cleanup();
            } catch {
                // noexcept
            }
        }
    }

    [HarmonyPostfix]
    internal static void OnSceneInit(Scene.Mode newMode)
    {
        if (newMode != Scene.Mode.StartGame) {
            return;
        }

        SafeToCreate = true;

        CoroutineHelper.Immediate(CustomChara.AddDelayedChara);
        CoroutineHelper.Immediate(CustomElement.GainAbilityOnLoad());

        CoroutineHelper.Deferred(
            () => SafeToCreate = false,
            () => EMono.core?.game is null);
    }
}