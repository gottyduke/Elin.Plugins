using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace Cdl.Patches;

[HarmonyPatch]
internal class LoadDramaPatch
{
    private static readonly Dictionary<string, string> _cachedSheets = [];

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(DramaManager), nameof(DramaManager.Load))]
    internal static IEnumerable<CodeInstruction> OnLoadIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(
                    typeof(ExcelData),
                    nameof(ExcelData.BuildList),
                    [typeof(string)])))
            .Repeat(cm => cm
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    Transpilers.EmitDelegate(BuildRelocatedList))
                .RemoveInstruction())
            .InstructionEnumeration();
    }


    internal static List<Dictionary<string, string>> BuildRelocatedList(ExcelData data, string oldPath,
        DramaManager dm)
    {
        var setup = dm.setup;

        var cachedBookName = $"{setup.book}_{Lang.langCode}";
        if (_cachedSheets.TryGetValue(cachedBookName, out var cachedPath)) {
            data.path = cachedPath;
            return data.BuildList(setup.sheet);
        }

        var sheets = BaseModManager.Instance.packages
            .Select(p => p.dirInfo)
            .SelectMany(d => Directory.GetFiles(d.FullName, "*.xlsx", SearchOption.AllDirectories))
            .Select(b => b.Replace('\\', '/'))
            .Where(b => b.Contains("Dialog/Drama/"));

        var books = sheets
            .Where(s => Path.GetFileNameWithoutExtension(s).Contains(setup.book))
            .OrderBy(b => b)
            .ToArray();

        var lang = Lang.langCode;
        var fallback = books.First();
        var localized = books.FirstOrDefault(b => b.Contains($"Lang/{lang}/") || b.Contains($"_{lang}")) ?? fallback;

        var path = lang switch {
            "EN" or "JP" => fallback,
            _ => localized,
        };

        _cachedSheets[cachedBookName] = path;
        data.path = path;
        return data.BuildList(setup.sheet);
    }
}