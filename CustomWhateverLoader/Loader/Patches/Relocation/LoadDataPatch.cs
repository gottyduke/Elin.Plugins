﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Loader.Patches.Relocation;

internal class LoadDataPatch
{
    private const string DefaultSheet = "_default";

    internal static IEnumerator LoadAllData()
    {
        MergeCharaTalk();
        MergeCharaTone();
        CoroutineHelper.Deferred(MergeGodTalk, () => Lang.setting is not null);

        yield break;
    }

    [Time]
    private static void MergeCharaTalk()
    {
        foreach (var charaTalk in PackageIterator.GetRelocatedExcelsFromPackage("Data/chara_talk.xlsx")) {
            MOD.listTalk.items.Add(charaTalk);
            CwlMod.Log("cwl_preload_chara_talk".Loc(charaTalk.path.ShortPath()));
        }
    }

    [Time]
    private static void MergeCharaTone()
    {
        foreach (var charaTone in PackageIterator.GetRelocatedExcelsFromPackage("Data/chara_tone.xlsx")) {
            MOD.tones.items.Add(charaTone);
            CwlMod.Log("cwl_preload_chara_tone".Loc(charaTone.path.ShortPath()));
        }
    }

    [Time]
    private static void MergeGodTalk()
    {
        var godTalk = EMono.sources.dataGodTalk;
        var map = godTalk.sheets[DefaultSheet].map.ToArray();

        foreach (var talk in PackageIterator.GetRelocatedExcelsFromPackage("Data/god_talk.xlsx", 3)) {
            try {
                foreach (var (topic, _) in map) {
                    if (topic is "") {
                        continue;
                    }

                    talk.sheets[DefaultSheet].map.GetValueOrDefault(topic)?
                        .Where(kv => kv.Key != "id")
                        .Do(kv => godTalk.sheets[DefaultSheet].map[topic].TryAdd(kv.Key, kv.Value));
                }

                CwlMod.Log("cwl_preload_god_talk".Loc(talk.path.ShortPath()));
            } catch (Exception ex) {
                CwlMod.Error("cwl_error_merge_god_talk".Loc(talk.path.ShortPath(), ex));
                // noexcept
            }
        }
    }
}