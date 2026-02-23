using System.Collections.Generic;
using System.Linq;
using ElinTogether.Helper;
using HarmonyLib;

namespace ElinTogether.Patches;

// hand of 105gun
[HarmonyPatch]
internal class FovCellLightOffsetPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Fov), nameof(Fov.ClearVisible))]
    internal static bool OnClearVisible(Fov __instance)
    {
        var remoteFovList = GetRemotePlayerCharaFovList();
        if (!remoteFovList.Contains(__instance)) {
            return true;
        }

        // build
        var shared = new HashSet<int>(
            remoteFovList
                .Where(fov => fov != __instance)
                .SelectMany(fov => fov.lastPoints.Keys)
        );

        // set cells as cleared but exclude shared fov cells
        foreach (var (pos, offset) in __instance.lastPoints) {
            var cell = Fov.map.GetCell(pos);

            cell.light -= offset;
            cell.lightR -= (ushort)(offset * __instance.r / 2);
            cell.lightG -= (ushort)(offset * __instance.g / 2);
            cell.lightB -= (ushort)(offset * __instance.b / 2);

            cell.pcSync = shared.Contains(pos);
        }

        __instance.lastPoints.Clear();
        return false;
    }

    private static HashSet<Fov> GetRemotePlayerCharaFovList()
    {
        var pc = EClass.pc;
        if (pc?.party?.members is null) {
            return [];
        }

        var list = new HashSet<Fov>();
        foreach (var chara in pc.party.members.Where(chara => chara.IsRemotePlayer)) {
            list.Add(chara.fov ??= chara.CreateFov());
        }

        return list;
    }
}