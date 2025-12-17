using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Cwl.Helper.Exceptions;
using LZ4;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Cwl.Helper.FileUtil;

public class ConfigCereal
{
    public static readonly JsonSerializerSettings Settings = new() {
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        PreserveReferencesHandling = PreserveReferencesHandling.None,
        TypeNameHandling = TypeNameHandling.Auto,
        ContractResolver = new WritablePropertiesOnlyResolver(),
    };

    public static void WriteConfig<T>(T data, string path)
    {
        WriteDataImpl(data, path, CompactLevel.TextIndent);
    }

    public static void WriteConfig<T>(T data, string path, JsonSerializerSettings settings)
    {
        WriteDataImpl(data, path, CompactLevel.TextIndent, settings);
    }

    public static void WriteData<T>(T data, string path)
    {
        WriteDataImpl(data, path, CompactLevel.TextFlat);
    }

    public static void WriteDataCompressed<T>(T data, string path)
    {
        WriteDataImpl(data, path, CompactLevel.Compress);
    }

    public static void WriteDataBinary<T>(T data, string path)
    {
        WriteDataImpl(data, path, CompactLevel.Binary);
    }

    public static bool ReadConfig<T>(string path, [NotNullWhen(true)] out T? inferred)
    {
        return ReadDataImpl(path, out inferred, CompactLevel.TextIndent);
    }

    public static bool ReadData<T>(string path, [NotNullWhen(true)] out T? inferred)
    {
        return ReadDataImpl(path, out inferred, CompactLevel.TextFlat);
    }

    public static bool ReadDataCompressed<T>(string path, [NotNullWhen(true)] out T? inferred)
    {
        return ReadDataImpl(path, out inferred, CompactLevel.Compress);
    }

    public static bool ReadDataBinary<T>(string path, [NotNullWhen(true)] out T? inferred)
    {
        return ReadDataImpl(path, out inferred, CompactLevel.Binary);
    }

    private static bool ReadDataImpl<T>(string path, [NotNullWhen(true)] out T? inferred, CompactLevel compact)
    {
        try {
            if (File.Exists(path)) {
                using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var js = JsonSerializer.CreateDefault(Settings);

                switch (compact) {
                    case CompactLevel.TextIndent:
                    case CompactLevel.TextFlat: {
                        using var sr = new StreamReader(fs);
                        using var jr = new JsonTextReader(sr);
                        inferred = js.Deserialize<T>(jr);
                        break;
                    }
                    case CompactLevel.Compress: {
                        using var lz4 = new LZ4Stream(fs, CompressionMode.Decompress);
                        using var sr = new StreamReader(lz4);
                        using var jr = new JsonTextReader(sr);
                        inferred = js.Deserialize<T>(jr);
                        break;
                    }
                    case CompactLevel.Binary: {
                        inferred = default;
                        return false;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(compact), compact, null);
                }

                return inferred is not null;
            }
        } catch (Exception ex) {
            CwlMod.Error<ConfigCereal>($"failed to read config: {ex.Message}");
            DebugThrow.Void(ex);
            // noexcept
        }

        inferred = default;
        return false;
    }

    private static void WriteDataImpl<T>(T data, string path, CompactLevel compact, JsonSerializerSettings? settings = null)
    {
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            using var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            var js = JsonSerializer.CreateDefault(settings ?? Settings);
            js.Formatting = compact is CompactLevel.TextIndent ? Formatting.Indented : Formatting.None;

            switch (compact) {
                case CompactLevel.TextIndent:
                case CompactLevel.TextFlat: {
                    using var sw = new StreamWriter(fs);
                    using var jw = new JsonTextWriter(sw);
                    js.Serialize(jw, data);
                    break;
                }
                case CompactLevel.Compress: {
                    using var lz4 = new LZ4Stream(fs, CompressionMode.Compress);
                    using var sw = new StreamWriter(lz4);
                    using var jw = new JsonTextWriter(sw);
                    js.Serialize(jw, data);
                    break;
                }
                case CompactLevel.Binary: {
                    return;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(compact), compact, null);
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
        Binary,
    }

    public class WritablePropertiesOnlyResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (!property.Writable) {
                property.ShouldSerialize = _ => false;
            }

            return property;
        }
    }
}