using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.Helper;
using HarmonyLib;

namespace Cwl.Patches.Sounds;

[HarmonyPatch]
internal class MergePlaylistPatch
{
    private const string GlobalName = "BGM/Playlist_Global";

    internal static MethodInfo TargetMethod()
    {
        return AccessTools.Method(typeof(Zone), nameof(Zone.CreatePlaylist));
    }

    [HarmonyPrefix]
    internal static void OnMergePlaylist(Zone __instance, ref List<int> list, Playlist? mold = null)
    {
        list.RemoveAll(id => !Core.Instance.refs.dictBGM.ContainsKey(id));

        if (mold != null) {
            return;
        }

        var mergeName = $"BGM/Playlist_{__instance.GetType().Name}";
        var merges = DataLoader.CachedSounds.Keys
            .Where(id => id.StartsWith(mergeName) || id.StartsWith(GlobalName));
        foreach (var merge in merges) {
            var id = ReverseId.BGM(merge);
            var reverseLookup = ReverseId.BGM(merge[(merge.LastIndexOf('/') + 1)..]);

            if (reverseLookup != -1 && reverseLookup <= DataLoader.LastBgmIndex) {
                for (var i = 0; i < list.Count; ++i) {
                    if (list[i] != reverseLookup) {
                        continue;
                    }

                    list[i] = id;
                    break;
                }
            } else {
                list.Add(id);
            }
        }

        list = list.Distinct().ToList();
    }

    [HarmonyPostfix]
    internal static void OnMoldPlaylist(Zone __instance, ref Playlist __result, Playlist? mold = null)
    {
        if (mold == null) {
            __result.name = $"Playlist_{__instance.GetType().Name}";
        }
    }
}