using System;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using MethodTimer;

namespace Cwl;

internal partial class DataLoader
{
    [Time]
    internal static void MergeCharaTalk()
    {
        foreach (var charaTalk in PackageIterator.GetRelocatedExcelsFromPackage("Data/chara_talk.xlsx")) {
            try {
                MOD.listTalk.items.Add(charaTalk);
                CwlMod.Log<DataLoader>("cwl_preload_chara_talk".Loc(charaTalk.path.ShortPath()));
            } catch (Exception ex) {
                CwlMod.Warn<DataLoader>("cwl_error_failure".Loc(ex));
                // noexcept
            }
        }
    }

    [Time]
    internal static void MergeCharaTone()
    {
        foreach (var charaTone in PackageIterator.GetRelocatedExcelsFromPackage("Data/chara_tone.xlsx")) {
            try {
                MOD.tones.items.Add(charaTone);
                CwlMod.Log<DataLoader>("cwl_preload_chara_tone".Loc(charaTone.path.ShortPath()));
            } catch (Exception ex) {
                CwlMod.Warn<DataLoader>("cwl_error_failure".Loc(ex));
                // noexcept
            }
        }
    }
}