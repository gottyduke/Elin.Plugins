using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using Newtonsoft.Json;

namespace Erpc.Resources;

internal static class LocHelper
{
    private static Dictionary<string, LocString> _lines = new();

    internal static string Loc(this string id)
    {
        if (!_lines.TryGetValue(id, out var lines)) {
            return id;
        }

        var line = GetLangCode() switch {
            "CN" => lines.Cn,
            "JP" => lines.Jp,
            "EN" => lines.En,
            _ => lines.En,
        };

        var rand = new Random();
        return line[rand.Next(line.Length)];
    }

    internal static bool LoadExternalLocs()
    {
        try {
            const string filename = "erpc_localization";
            var @override = Path.Combine(Paths.ConfigPath, $"{filename}_{ModInfo.Version}.json");
            if (!File.Exists(@override)) {
                var @base = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, $"{filename}.json");
                File.Copy(@base, @override);
                ErpcMod.Log($"updated localizations to {ModInfo.Version}");
            }

            using var sr = new StreamReader(@override);
            _lines = JsonConvert.DeserializeObject<Dictionary<string, LocString>>(sr.ReadToEnd()) ?? new();
        } catch (Exception ex) {
            ErpcMod.Log($"failed to load localizations: {ex.Message}");
            return false;
        }

        return true;
    }

    internal static string GetLangCode()
    {
        var lc = ErpcConfig.LangCodeOverride?.Value ?? "GAME";
        if (lc == "GAME") {
            lc = Lang.langCode;
        }

        return lc;
    }

    private sealed record LocString(string[] Cn, string[] Jp, string[] En);
}