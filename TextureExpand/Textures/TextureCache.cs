using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TextureExpand.Rules;
using UnityEngine;

namespace TextureExpand.Textures;

internal static class TextureCache
{
    private const string CacheFolder = ".Cache";
    private const string LogFile = "log.txt";

    internal static string Dir = "";

    internal static bool Enabled;

    private static string CachePath => Path.Combine(Dir, CacheFolder);
    private static string LogPath => Path.Combine(CachePath, LogFile);

    internal static bool Validate(DateTime latestImage)
    {
        if (!File.Exists(LogPath)) {
            return false;
        }

        using var reader = new StreamReader(LogPath);
        if (!DateTime.TryParse(reader.ReadLine(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var written) ||
            written < latestImage) {
            return false;
        }

        var lines = 0;
        while (reader.ReadLine() is not null) {
            lines++;
        }

        return lines == RuleRegistry.Count;
    }

    internal static Texture2D? TryLoad(CustomRenderData data, Texture2D source)
    {
        var path = Path.Combine(CachePath, data.TextureName + ".png");
        if (!File.Exists(path)) {
            Enabled = false;
            TextureExpand.LogError("Not found cache file: " + path);
            return null;
        }

        var texture = new Texture2D(data.TextureWidth, data.TextureHeight, source.format, source.mipmapCount > 1);
        texture.LoadImage(File.ReadAllBytes(path));
        data.UseCache = true;
        return texture;
    }

    internal static void Write()
    {
        Directory.CreateDirectory(CachePath);

        var sb = new StringBuilder(1000);
        sb.AppendLine(DateTime.Now.ToString("O"));

        foreach (var kv in RuleRegistry.ByTile) {
            foreach (var rule in kv.Value) {
                sb.Append("tileId: ").Append(kv.Key).Append(", rule: ").Append(rule.ConcatMatchPhrase)
                    .Append(", file: ").AppendLine(rule.ConcatFilePath);
            }
        }

        foreach (var kv in RuleRegistry.ByCharaId) {
            foreach (var rule in kv.Value) {
                sb.Append("charaId: ").Append(kv.Key).Append(", rule: ").Append(rule.ConcatMatchPhrase)
                    .Append(", file: ").AppendLine(rule.ConcatFilePath);
            }
        }

        File.WriteAllText(LogPath, sb.ToString());

        foreach (var data in RenderDataExpander.CustomRenderDatas.Values.SelectMany(x => x)) {
            File.WriteAllBytes(Path.Combine(CachePath, data.TextureName + ".png"), data.Texture!.EncodeToPNG());
        }
    }
}