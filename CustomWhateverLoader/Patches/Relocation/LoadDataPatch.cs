using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.File;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Relocation;

[HarmonyPatch]
internal class LoadDataPatch
{
    private const string DefaultSheet = "_default";

    [Time]
    internal static IEnumerator LoadAllData()
    {
        MergeCharaTalk();
        MergeCharaTone();
        MergeGodTalk();

        yield break;
    }

    private static void MergeCharaTalk()
    {
        foreach (var charaTalk in PackageFileIterator.GetRelocatedExcelsFromPackage("Data/chara_talk.xlsx")) {
            MOD.listTalk.items.Add(charaTalk);
            CwlMod.Log("cwl_preload_chara_talk".Loc(charaTalk.path.ShortPath()));
        }
    }

    private static void MergeCharaTone()
    {
        foreach (var charaTone in PackageFileIterator.GetRelocatedExcelsFromPackage("Data/chara_tone.xlsx")) {
            MOD.tones.items.Add(charaTone);
            CwlMod.Log("cwl_preload_chara_tone".Loc(charaTone.path.ShortPath()));
        }
    }

    private static void MergeGodTalk()
    {
        var godTalk = EMono.sources.dataGodTalk;
        var map = godTalk.sheets[DefaultSheet].map.ToArray();

        foreach (var file in PackageFileIterator.GetRelocatedFilesFromPackage("Data/god_talk.xlsx")) {
            try {
                var talk = new ExcelData(file.FullName, 3);

                foreach (var (topic, _) in map) {
                    if (topic is "") {
                        continue;
                    }

                    talk.sheets[DefaultSheet].map.GetValueOrDefault(topic)?
                        .Where(kv => kv.Key != "id")
                        .Do(kv => godTalk.sheets[DefaultSheet].map[topic].TryAdd(kv.Key, kv.Value));
                }

                CwlMod.Log("cwl_preload_god_talk".Loc(file.ShortPath()));
            } catch (Exception ex) {
                CwlMod.Error("cwl_error_merge_god_talk".Loc(file.ShortPath(), ex));
                // noexcept
            }
        }
    }
}