using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Cwl.Helper.Extensions;
using HarmonyLib;
using UnityEngine;

namespace ElinTogether.Patches;

[HarmonyPatch]
internal class FovCellLightOffsetPatch
{
    public static List<Fov> GetRemotePlayerCharaFovs()
    {
        List<Fov> list = new();
        if (EClass.pc?.party == null)
        {
            return list;
        }

        // TODO: dk will fix it
        foreach (var chara in EClass.pc.party.members)
        {
            if (chara != null)
            {
                if (chara.fov == null)
                {
                    EmpLog.Warning($"GetRemotePlayerCharaFovs chara {chara.Name} has no fov");
                    chara.fov = chara.CreateFov();
                }
                list.Add(chara.fov);
            }
        }
        return list;
    }

    static byte GetClearVisibleOffset(byte original, Fov fov, int key, ref bool found)
    {
		Cell cell = Fov.map.GetCell(key);
        byte currentLightValue = cell.light;
        byte targetLightValue = 0;
        found = false;
        foreach (var f in GetRemotePlayerCharaFovs())
        {
            if (f == fov)
            {
                continue;
            }
            if (f.lastPoints.ContainsKey(key))
            {
                targetLightValue = targetLightValue > f.lastPoints[key] ? targetLightValue : f.lastPoints[key];
                found = true;
            }
        }
        byte newValue = (byte)(currentLightValue - targetLightValue);
        EmpLog.Verbose($"GetClearVisibleOffset {fov} {original} {key} current {currentLightValue} target {targetLightValue} newValue: {newValue}");
        return newValue;
    }

    [HarmonyPatch(typeof(Fov), nameof(Fov.ClearVisible))]
    [HarmonyPrefix]
    internal static bool OnClearVisible(Fov __instance)
    {
        List<Fov> RemoteFovList = GetRemotePlayerCharaFovs();
        if (RemoteFovList.Contains(__instance))
        {
            foreach (KeyValuePair<int, byte> lastPoint in __instance.lastPoints)
            {
                Cell cell = Fov.map.GetCell(lastPoint.Key);
                bool found = false;
                byte value = lastPoint.Value;
                cell.light -= value;
                cell.lightR -= (ushort)(value * __instance.r / 2);
                cell.lightG -= (ushort)(value * __instance.g / 2);
                cell.lightB -= (ushort)(value * __instance.b / 2);

                // set false only when no remote player chara can see it
                foreach (var f in RemoteFovList)
                {
                    if (f == __instance)
                    {
                        continue;
                    }
                    if (f.lastPoints.ContainsKey(lastPoint.Key))
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    cell.pcSync = false;
                }
            }
            __instance.lastPoints.Clear();
            return false;
        }
        return true;
    }
}