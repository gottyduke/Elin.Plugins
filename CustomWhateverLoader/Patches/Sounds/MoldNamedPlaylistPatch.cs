using System.Collections.Generic;
using System.Linq;
using Cwl.API.Custom;
using HarmonyLib;

namespace Cwl.Patches.Sounds;

[HarmonyPatch(typeof(Zone), nameof(Zone.CreatePlaylist))]
internal class MoldNamedPlaylistPatch
{
    [SwallowExceptions]
    [HarmonyPrefix]
    internal static void PurgePlaylist(ref List<int> list)
    {
        list.RemoveAll(id => !Core.Instance.refs.dictBGM.ContainsKey(id));
    }

    [SwallowExceptions]
    [HarmonyPostfix]
    internal static void OnMoldPlaylist(Zone __instance, ref Playlist __result, ref List<int> list, Playlist? mold = null)
    {
        var zoneTypeName = __instance.GetType().Name;
        __result.name = mold != null
            ? $"{mold.name}_{zoneTypeName}/"
            : $"Playlist_Blank_{zoneTypeName}/";
        __result.name += string.Join("/", list.OrderBy(i => i));

        CwlMod.Debug<CustomPlaylist>($"molding playlist {__result.name} for {__instance.GetType().Name}");
    }
}