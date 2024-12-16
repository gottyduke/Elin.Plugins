using System;
using System.IO;
using Newtonsoft.Json;

namespace Cwl.Helper.FileUtil;

public static class ConfigCereal
{
    public static void WriteConfig<T>(T data, string path)
    {
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            using var sw = new StreamWriter(path);
            sw.Write(JsonConvert.SerializeObject(data, Formatting.Indented));
        } catch (Exception ex) {
            CwlMod.Error($"internal failure: {ex.Message}");
            // noexcept
        }
    }

    public static bool ReadConfig<T>(string path, out T? inferred)
    {
        try {
            if (File.Exists(path)) {
                using var sr = new StreamReader(path);
                inferred = JsonConvert.DeserializeObject<T>(sr.ReadToEnd());
                return true;
            }
        } catch (Exception ex) {
            CwlMod.Error($"failed to read config: {ex.Message}");
            throw;
        }

        inferred = default;
        return false;
    }
}