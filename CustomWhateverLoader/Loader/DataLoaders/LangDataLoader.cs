using System;
using System.Linq;
using Cwl.LangMod;
using MethodTimer;

namespace Cwl;

internal partial class DataLoader
{
    [Time]
    internal static void MergeCharaTalk()
    {
        var excels = PackageIterator.GetFiles("Data/chara_talk.xlsx")
            .Select(f => new ExcelData(f.FullName));
        foreach (var charaTalk in excels) {
            try {
                MOD.listTalk.items.Add(charaTalk);
                CwlMod.Log<DataLoader>("cwl_preload_chara_talk".Loc(charaTalk.path.ShortPath()));
            } catch (Exception ex) {
                CwlMod.WarnWithPopup<DataLoader>("cwl_error_failure".Loc(ex.Message), ex);
                // noexcept
            }
        }
    }

    [Time]
    internal static void MergeCharaTone()
    {
        var excels = PackageIterator.GetFiles("Data/chara_tone.xlsx")
            .Select(f => new ExcelData(f.FullName));
        foreach (var charaTone in excels) {
            try {
                MOD.tones.items.Add(charaTone);
                CwlMod.Log<DataLoader>("cwl_preload_chara_tone".Loc(charaTone.path.ShortPath()));
            } catch (Exception ex) {
                CwlMod.WarnWithPopup<DataLoader>("cwl_error_failure".Loc(ex.Message), ex);
                // noexcept
            }
        }
    }
}