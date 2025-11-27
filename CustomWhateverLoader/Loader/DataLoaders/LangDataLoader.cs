using System;
using System.IO;
using Cwl.Helper.Extensions;
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
        foreach (var charaTalk in PackageIterator.GetExcelsFromPackage("Data/chara_talk.xlsx")) {
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
        foreach (var charaTone in PackageIterator.GetExcelsFromPackage("Data/chara_tone.xlsx")) {
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

        foreach (var alias in PackageIterator.GetExcelsFromPackage("Data/Alias.xlsx")) {
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

        foreach (var names in PackageIterator.GetExcelsFromPackage("Data/Name.xlsx")) {
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