using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ElinTogether.Helper.Extensions;
using ElinTogether.Models;
using ElinTogether.Net;
using HarmonyLib;
using UnityEngine;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal static class Synchronization
{
    internal static float GameDelta { get; set; }
    internal static bool CanSendDelta { get; set; }
    internal static int RefSpeed { get; set; }

    internal static void AllowDeltaSending()
    {
        CanSendDelta = true;
    }

    [HarmonyPatch]
    internal static class CharaSynchronizationContext
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Chara), nameof(Chara._Move))]
        internal static IEnumerable<CodeInstruction> OnCharaMove(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new OperandContains(OpCodes.Stfld, nameof(Chara.actTime)))
                .EnsureValid("Chara._Move set field actTime")
                .SetInstructionAndAdvance(
                    Transpilers.EmitDelegate(SetActTime))
                .InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Chara), nameof(Chara.Tick))]
        internal static IEnumerable<CodeInstruction> OnCharaTick(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new OperandContains(OpCodes.Stfld, nameof(Chara.actTime)))
                .EnsureValid("Chara.Tick set field actTime 1")
                .SetInstructionAndAdvance(
                    Transpilers.EmitDelegate(SetActTime))
                .MatchStartForward(
                    new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(Chara), nameof(Chara.actTime))))
                .EnsureValid("Chara.Tick set field actTime 2")
                .InsertAndAdvance(
                    new(OpCodes.Pop),
                    Transpilers.EmitDelegate(() => EClass.player.baseActTime))
                .SetInstructionAndAdvance(
                    Transpilers.EmitDelegate(SetActTime))
                .InstructionEnumeration();
        }

        private static void SetActTime(Chara chara, float num)
        {
            chara.actTime = num * Mathf.Max(0.1f, (float)RefSpeed / chara.Speed);
        }
    }

    [HarmonyPatch(typeof(Core), nameof(Core.Update))]
    internal static class CoreSynchronizationContext
    {
        [HarmonyPrefix]
        internal static void OnCoreUpdate()
        {
            // apply remote delta happened in previous updates before this update
            switch (NetSession.Instance.Connection) {
                case ElinNetHost host:
                    host.WorldStateDeltaProcess();
                    return;
                case ElinNetClient client:
                    GameDelta = 0f;
                    client.WorldStateDeltaProcess();
                    return;
            }
        }

        [HarmonyPostfix]
        internal static void OnCoreUpdateEnd()
        {
            CardCache.Update();
            switch (NetSession.Instance.Connection) {
                case ElinNetHost host:
                    if (!EMono.scene.paused) {
                        host.Delta.AddRemote(new GameDelta {
                            Delta = Core.gameDelta,
                        });
                    }

                    if (CanSendDelta) {
                        CanSendDelta = false;
                        host.WorldStateDeltaUpdate();
                    }

                    return;
                case ElinNetClient client when CanSendDelta:
                    CanSendDelta = false;
                    client.WorldStateDeltaUpdate();
                    return;
            }
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.OnUpdate))]
    internal static class GameSynchronizationContext
    {
        [HarmonyPrefix]
        internal static void OnGameOnUpdate()
        {
            switch (NetSession.Instance.Connection) {
                // apply game delta as clients
                case ElinNetClient:
                    Core.gameDelta = GameDelta;
                    break;
                // allow remote players to trigger turbo
                case ElinNetHost when !EMono.scene.paused:
                    ActionMode.Adv.SetTurbo();
                    break;
                default:
                    RefSpeed = EClass.pc.Speed;
                    return;
            }

            if (NetSession.Instance.CurrentPlayers.All(n => n.Speed == 0)) {
                RefSpeed = EClass.pc.Speed;
                return;
            }

            if (NetSession.Instance.Rules.UseSharedSpeed) {
                RefSpeed = NetSession.Instance.SharedSpeed;
            } else {
                var min = (float)NetSession.Instance.CurrentPlayers.Where(n => n.Speed > 0).Min(n => n.Speed);
                var max = (float)NetSession.Instance.CurrentPlayers.Max(n => n.Speed);
                var mult = Math.Sqrt(max / min);

                mult = Math.Min(mult, 8f);

                RefSpeed = (int)(max / mult);
            }
        }
    }
}