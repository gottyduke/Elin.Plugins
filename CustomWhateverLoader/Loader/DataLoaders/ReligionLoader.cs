using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.API;
using Cwl.API.Custom;
using Cwl.Helper.Extensions;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
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
    internal static void MergeGodTalk()
    {
        var godTalk = EMono.sources.dataGodTalk;
        var map = godTalk.sheets[DefaultSheet].map;

        foreach (var talk in PackageIterator.GetRelocatedExcelsFromPackage("Data/god_talk.xlsx", 3)) {
            try {
                foreach (var (topic, _) in map) {
                    if (topic == "") {
                        continue;
                    }

                    CwlMod.CurrentLoading = $"[CWL] GodTalk/{talk.path.ShortPath()}";

                    talk.sheets[DefaultSheet].map.GetValueOrDefault(topic)?
                        .Where(kv => kv.Key != "id")
                        .Do(map[topic].TryAdd);
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
    internal static void MergeFactionElements()
    {
        var elements = PackageIterator.GetRelocatedJsonsFromPackage<SerializableReligionElement>("Data/religion_elements.json");
        foreach (var (path, element) in elements) {
            try {
                foreach (var (id, list) in element) {
                    if (!CustomReligion.Managed.TryGetValue(id, out var custom)) {
                        continue;
                    }

                    custom.SetElements(list);
                    CwlMod.Log<DataLoader>("cwl_log_god_elements".Loc(list.Length, custom.id));
                }
            } catch (Exception ex) {
                CwlMod.ErrorWithPopup<DataLoader>("cwl_error_merge_god_elements".Loc(path.ShortPath(), ex.Message), ex);
                // noexcept
            }
        }
    }
}