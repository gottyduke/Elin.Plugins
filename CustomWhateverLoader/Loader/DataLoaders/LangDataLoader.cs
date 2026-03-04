using System;
using System.IO;
using System.Linq;
using Cwl.Helper.Extensions;
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

    [Time]
    internal static void MergeCustomAlias()
    {
        AliasGen.list = null!;
        AliasGen.Init();

        var excels = PackageIterator.GetFiles("Data/Alias.xlsx")
            .Select(f => new ExcelData(f.FullName));
        foreach (var alias in excels) {
            try {
                var newRules = alias.BuildList();
                AliasGen.list.AddRange(newRules);

                var mixRule = IO.LoadTextArray($"{Path.GetDirectoryName(Lang.alias.path)}/Alias_mix.txt");
                foreach (var mix in mixRule) {
                    var rule = mix.Split(',');
                    if (rule.Length >= 2) {
                        AliasGen.listMix.Add(new() {
                            chance = rule[0].AsInt(0),
                            texts = rule[1].Split('+'),
                        });
                    }
                }

                CwlMod.Log<DataLoader>($"added {newRules.Count}/{mixRule.Length} alias rules from {alias.path.ShortPath()}");
            } catch (Exception ex) {
                CwlMod.WarnWithPopup<DataLoader>("cwl_error_failure".Loc(ex.Message), ex);
                // noexcept
            }
        }
    }

    [Time]
    internal static void MergeCustomName()
    {
        NameGen.list = null!;
        NameGen.Init();

        var excels = PackageIterator.GetFiles("Data/Name.xlsx")
            .Select(f => new ExcelData(f.FullName));
        foreach (var names in excels) {
            try {
                var newNames = names.BuildList();
                NameGen.list.AddRange(newNames);

                CwlMod.Log<DataLoader>($"added {newNames.Count} names from {names.path.ShortPath()}");
            } catch (Exception ex) {
                CwlMod.WarnWithPopup<DataLoader>("cwl_error_failure".Loc(ex.Message), ex);
                // noexcept
            }
        }
    }
}