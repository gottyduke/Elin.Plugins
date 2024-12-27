using System;
using Cwl.API.Processors;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Loader.Patches.Quests;

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
        CwlMod.Warn("cwl_warn_deserialize".Loc(nameof(Quest), qualified, readType.MetadataToken,
            CwlConfig.Patches.SafeCreateClass!.Definition.Key));

        if (!_cleanup) {
            CoroutineHelper.Deferred(PostCleanup, () => EClass.game.isLoading);
        }

        _cleanup = true;
    }

    private static void PostCleanup()
    {
        var list = EClass.game.quests.globalList;
        list.ForeachReverse(q => {
            if (EMono.sources.quests.map.ContainsKey(q.id)) {
                return;
            }

            list.Remove(q);
            CwlMod.Log("cwl_log_post_cleanup".Loc(nameof(Quest), q.id));
        });
    }
}