using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Cwl.Helper.File;
using HarmonyLib;
using MethodTimer;

namespace Cwl.Patches.Relocation;

[HarmonyPatch]
internal class LoadDialogPatch
{
    private const string CacheEntry = "Dialog";
    private const string Pattern = "*.xlsx";

    private static readonly List<ExcelData> _cached = [];

    [HarmonyTargetMethods]
    internal static IEnumerable<MethodInfo> DialogBuildMap()
    {
        return [
            AccessTools.Method(typeof(DramaCustomSequence), nameof(DramaCustomSequence.HasTopic)),
            AccessTools.Method(typeof(Lang), nameof(Lang.GetDialog)),
        ];
    }

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OnLoadIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(
                    typeof(ExcelData),
                    nameof(ExcelData.BuildMap))))
            .SetInstructionAndAdvance(
                Transpilers.EmitDelegate(BuildRelocatedMap))
            .InstructionEnumeration();
    }

    [Time]
    private static void BuildRelocatedMap(ExcelData data, string sheetName)
    {
        data.BuildMap(sheetName);

        foreach (var cache in _cached) {
            cache.BuildMap(sheetName);
            foreach (var (topic, cells) in cache.sheets[sheetName].map) {
                if (topic is null or "") {
                    continue;
                }

                data.sheets[sheetName].map.TryAdd(topic, cells);
            }
        }
    }

    [Time]
    internal static IEnumerator LoadAllDialogs()
    {
        var books = PackageFileIterator.GetLangModFilesFromPackage()
            .SelectMany(d => d.GetDirectories(CacheEntry))
            .SelectMany(d => d.GetFiles(Pattern, SearchOption.TopDirectoryOnly))
            .Where(f => Path.GetFileNameWithoutExtension(f.Name) == "dialog");

        foreach (var book in books) {
            try {
                var data = new ExcelData {
                    path = book.FullName,
                };
                _cached.Add(data);
            } catch (Exception ex) {
                CwlMod.Error($"failed to load dialog {book.FullName}\n{ex.Message}");
                continue;
                // noexcept
            }

            CwlMod.Log($"loaded relocated dialog {book.FullName}");
        }

        yield return null;
    }
}