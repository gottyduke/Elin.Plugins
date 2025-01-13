using System;
using Cwl.API.Processors;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Quests;

[HarmonyPatch]
internal class SafeCreateQuestPatch
{
    private static bool _cleanup;

    internal static bool Prepare()
    {
        if (CwlConfig.SafeCreateClass) {
            TypeResolver.Add(ResolveQuest);
        }

        return CwlConfig.SafeCreateClass;
    }

    // 23.73 fixed
    // https://elin-modding-resources.github.io/Elin.Docs/diff/424dbab1c12832c2e79eb2d9f3c9fd4d8cf56696
    private static void ResolveQuest(ref bool resolved, Type objectType, ref Type readType, string qualified)
    {
        if (resolved) {
            return;
        }

        if (objectType != typeof(Quest) || readType != typeof(object)) {
            return;
        }

        readType = typeof(Quest);
        resolved = true;
        CwlMod.Warn<Quest>("cwl_warn_deserialize".Loc(nameof(Quest), qualified, readType.MetadataToken,
            CwlConfig.Patches.SafeCreateClass!.Definition.Key));

        if (!_cleanup) {
            SafeSceneInitPatch.Cleanups.Enqueue(PostCleanup);
        }

        _cleanup = true;
    }

    [SwallowExceptions]
    private static void PostCleanup()
    {
        if (!_cleanup) {
            return;
        }

        var list = EClass.game.quests.globalList;
        list.ForeachReverse(q => {
            if (EMono.sources.quests.map.ContainsKey(q.id)) {
                return;
            }

            list.Remove(q);
            CwlMod.Log<Quest>("cwl_log_post_cleanup".Loc(nameof(Quest), q.id));
        });
    }
}