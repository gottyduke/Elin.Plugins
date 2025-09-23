using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.API;
using Cwl.API.Attributes;
using Cwl.API.Custom;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;
using ReflexCLI.Attributes;

namespace Cwl;

internal partial class DataLoader
{
    private const string DefaultSheet = "_default";

    [Time]
    [ConsoleCommand("load_god_talk")]
    [CwlSourceReloadEvent]
    internal static void MergeGodTalk()
    {
        if (Lang.setting?.dir is null) {
            CoroutineHelper.Deferred(MergeGodTalk, () => Lang.setting?.dir is not null);
            return;
        }

        var godTalk = EMono.sources.dataGodTalk;
        var map = godTalk.sheets[DefaultSheet].map;
        foreach (var talk in PackageIterator.GetExcelsFromPackage("Data/god_talk.xlsx", 3)) {
            try {
                foreach (var (topic, _) in map) {
                    if (topic == "") {
                        continue;
                    }

                    CwlMod.CurrentLoading = $"[CWL] GodTalk/{talk.path.ShortPath()}";

                    talk.sheets[DefaultSheet].map.GetValueOrDefault(topic)?
                        .Where(kv => kv.Key != "id")
                        .Do(kv => map[topic][kv.Key] = kv.Value);
                }

                CwlMod.Log<DataLoader>("cwl_preload_god_talk".Loc(talk.path.ShortPath()));
            } catch (Exception ex) {
                CwlMod.ErrorWithPopup<DataLoader>("cwl_error_merge_god_talk".Loc(talk.path.ShortPath(), ex.Message), ex);
                // noexcept
            }
        }
    }

    [Time]
    [ConsoleCommand("load_religion_elements")]
    [CwlSourceReloadEvent]
    internal static void MergeFactionElements()
    {
        var elements = PackageIterator.GetJsonsFromPackage<SerializableReligionElement>("Data/religion_elements.json")
            .SelectMany(json => json.Item2)
            .GroupBy(kv => kv.Key, kv => kv.Value)
            .ToDictionary(g => g.Key, g => g.SelectMany(values => values).ToArray());
        foreach (var (id, custom) in CustomReligion.Managed) {
            if (!elements.TryGetValue(id, out var element)) {
                continue;
            }

            var factionElements = element.Distinct().ToArray();
            custom.SetElements(factionElements);
            CwlMod.Log<DataLoader>("cwl_log_god_elements".Loc(factionElements.Length, custom.id));
        }
    }

    [Time]
    [ConsoleCommand("load_religion_offerings")]
    [CwlSourceReloadEvent]
    internal static void MergeOfferingMultiplier()
    {
        var offerings = PackageIterator.GetJsonsFromPackage<SerializableReligionOffering>("Data/religion_offerings.json")
            .SelectMany(json => json.Item2)
            .GroupBy(kv => kv.Key, kv => kv.Value)
            .ToDictionary(g => g.Key, g => g.Last());

        foreach (var (id, custom) in CustomReligion.Managed) {
            if (!offerings.TryGetValue(id, out var offering)) {
                continue;
            }

            custom.SetOfferingMtp(offering);
            CwlMod.Log<DataLoader>("cwl_log_god_offerings".Loc(offering.Count, custom.id));
        }
    }
}