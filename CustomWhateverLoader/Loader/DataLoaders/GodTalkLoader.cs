using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.Extensions;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl;

internal partial class DataLoader
{
    private const string DefaultSheet = "_default";

    [Time]
    internal static IEnumerator MergeGodTalk()
    {
        var godTalk = EMono.sources.dataGodTalk;
        var map = godTalk.sheets[DefaultSheet].map;

        foreach (var talk in PackageIterator.GetRelocatedExcelsFromPackage("Data/god_talk.xlsx", 3)) {
            try {
                foreach (var (topic, _) in map) {
                    if (topic is "") {
                        continue;
                    }

                    CwlMod.CurrentLoading = $"[CWL] GodTalk/{talk.path.ShortPath()}";

                    talk.sheets[DefaultSheet].map.GetValueOrDefault(topic)?
                        .Where(kv => kv.Key != "id")
                        .Do(map[topic].TryAdd);
                }

                CwlMod.Log<DataLoader>("cwl_preload_god_talk".Loc(talk.path.ShortPath()));
            } catch (Exception ex) {
                CwlMod.Error<DataLoader>("cwl_error_merge_god_talk".Loc(talk.path.ShortPath(), ex));
                // noexcept
            }

            yield return null;
        }
    }
}