using System.Collections.Generic;
using System.Reflection;
using Cwl.API.Custom;
using HarmonyLib;

namespace Cwl.Patches.Sounds;

[HarmonyPatch]
internal class MoldNamedPlaylistPatch
{
    internal static MethodInfo TargetMethod()
    {
        return AccessTools.Method(typeof(Zone), nameof(Zone.CreatePlaylist));
    }

    [SwallowExceptions]
    [HarmonyPrefix]
    internal static void PurgePlaylist(ref List<int> list)
    {
        list.RemoveAll(id => !Core.Instance.refs.dictBGM.ContainsKey(id));
    }

    [SwallowExceptions]
    [HarmonyPostfix]
    internal static void OnMoldPlaylist(Zone __instance, ref Playlist __result, Playlist? mold = null)
    {
        var zoneTypeName = __instance.GetType().Name;
        __result.name = mold != null
            ? $"{mold.name}_{zoneTypeName}"
            : $"Playlist_Blank_{zoneTypeName}";

        CwlMod.Debug<CustomPlaylist>($"molding playlist {__result.name} for {__instance.GetType().Name}");
    }
}