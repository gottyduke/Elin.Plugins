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
        var sources = EMono.sources;
        var quests = EMono.game.quests;
        HashSet<Quest> list = [..quests.globalList, ..quests.list];

        foreach (var quest in list) {
            switch (quest) {
                case QuestDummy when !sources.quests.map.ContainsKey(quest.id):
                case QuestDeliver deliver when !sources.things.map.ContainsKey(deliver.idThing):
                    quests.list.Remove(quest);
                    quests.globalList.Remove(quest);
                    break;
                default:
                    continue;
            }

            CwlMod.Log<Quest>("cwl_log_post_cleanup".Loc(quest.GetType().Name, quest.id));
        }
    }
}