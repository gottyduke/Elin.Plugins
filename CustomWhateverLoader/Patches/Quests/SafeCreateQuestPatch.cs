using System;
using System.Collections.Generic;
using Cwl.API;
using Cwl.API.Processors;
using Cwl.LangMod;
using HarmonyLib;

namespace Cwl.Patches.Quests;

[HarmonyPatch]
internal class SafeCreateQuestPatch
{
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
    }

    [SwallowExceptions]
    [CwlPostLoad]
    private static void PostCleanup()
    {
        var quests = EMono.game.quests;
        HashSet<Quest> list = [..quests.globalList, ..quests.list];

        foreach (var quest in list) {
            switch (quest) {
                // 1.19.8 purge dummies regardless
                case QuestDummy:
                case not null when quest.id is null || !EMono.sources.quests.map.ContainsKey(quest.id):
                    quests.list.Remove(quest);
                    quests.globalList.Remove(quest);
                    CwlMod.Log<Quest>("cwl_log_post_cleanup".Loc(quest.GetType().Name, quest.id));
                    break;
                default:
                    continue;
            }
        }
    }
}