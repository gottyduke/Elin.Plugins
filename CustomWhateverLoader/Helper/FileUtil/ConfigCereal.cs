using System;
using System.IO;
using System.IO.Compression;
using LZ4;
using Newtonsoft.Json;

namespace Cwl.Helper.FileUtil;

public class ConfigCereal
{
    public static void WriteConfig<T>(T data, string path)
    {
        WriteDataImpl(data, path);
    }

    public static void WriteData<T>(T data, string path)
    {
        WriteDataImpl(data, path, CompactLevel.TextFlat);
    }

    public static void WriteDataCompressed<T>(T data, string path)
    {
        WriteDataImpl(data, path, CompactLevel.Compress);
    }

    public static bool ReadConfig<T>(string? path, out T? inferred)
    {
        return ReadDataImpl(path, out inferred);
    }

    public static bool ReadData<T>(string? path, out T? inferred)
    {
        return ReadDataImpl(path, out inferred, true);
    }

    private static bool ReadDataImpl<T>(string? path, out T? inferred, bool compressed = false)
    {
        try {
            if (File.Exists(path)) {
                using var fs = File.Open(path!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                string data;
                if (!compressed) {
                    using var sr = new StreamReader(fs);
                    data = sr.ReadToEnd();
                } else {
                    using var lz4 = new LZ4Stream(fs, CompressionMode.Decompress);
                    using var sr = new StreamReader(lz4);
                    data = sr.ReadToEnd();
                }

                inferred = JsonConvert.DeserializeObject<T>(data);
                return inferred is not null;
            }
        } catch (Exception ex) {
            CwlMod.Error<ConfigCereal>($"failed to read config: {ex.Message}");
            throw;
        }

        inferred = default;
        return false;
    }

    private static void WriteDataImpl<T>(T data, string path, CompactLevel compact = CompactLevel.TextIndent)
    {
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var fmt = compact == CompactLevel.TextIndent ? Formatting.Indented : Formatting.None;
            var json = JsonConvert.SerializeObject(data, fmt);
            using var fs = new FileStream(path, FileMode.Create);

            if (compact == CompactLevel.Compress) {
                using var lz4 = new LZ4Stream(fs, CompressionMode.Compress);
                using var sw = new StreamWriter(lz4);
                sw.Write(json);
            } else {
                using var sw = new StreamWriter(fs);
                sw.Write(json);
            }
        } catch (Exception ex) {
            CwlMod.Error<ConfigCereal>($"internal failure: {ex}");
            // noexcept
        }
    }

    private enum CompactLevel
    {
        TextIndent,
        TextFlat,
        Compress,
    }
}