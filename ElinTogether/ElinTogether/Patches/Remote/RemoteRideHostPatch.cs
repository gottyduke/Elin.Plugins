using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class RemoteRideHostPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BaseGameScreen), nameof(BaseGameScreen.RefreshPosition))]
    internal static void OnSetHostRideFocus(BaseGameScreen __instance)
    {
        if (EClass.pc.host is { } host) {
            EClass.player.position = host.renderer.position;
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BaseTileMap), nameof(BaseTileMap.DrawTile))]
    internal static IEnumerable<CodeInstruction> OnDrawTileIl(IEnumerable<CodeInstruction> instructions)
    {
        // if (!chara.IsPC && !chara.renderer.IsMoving && this.detail.charas.Count > 1 && (this.detail.charas.Count != 2 || !this.detail.charas[0].IsDeadOrSleeping || !this.detail.charas[0].IsPCC))
        // {
        //     this._actorPos += this.renderSetting.charaPos[1 + ((num29 < 4) ? num29 : 3)];
        // }
        return new CodeMatcher(instructions)
            .MatchStartForward(
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Card), nameof(Chara.IsPC))))
            .SetInstruction(
                Transpilers.EmitDelegate((Chara chara) => chara.IsPC || EClass.pc.host == chara))
            .InstructionEnumeration();
    }
}