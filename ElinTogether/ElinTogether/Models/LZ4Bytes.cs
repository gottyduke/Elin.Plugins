using System.IO;
using System.IO.Compression;
using System.Text;
using LZ4;
using MessagePack;
using Newtonsoft.Json;

namespace ElinTogether.Models;

[MessagePackObject]
public class LZ4Bytes
{
    private static readonly JsonSerializer _serializer = JsonSerializer.Create(GameIO.jsReadGame);

    [Key(0)]
    public required byte[] Bytes { get; init; }

    public static LZ4Bytes Create(object data)
    {
        using var ms = new MemoryStream();
        using var lz4 = new LZ4Stream(ms, CompressionMode.Compress);
        using var sw = new StreamWriter(lz4, Encoding.UTF8);
        using var jw = new JsonTextWriter(sw);

        _serializer.Serialize(jw, data);
        jw.Flush();
        sw.Flush();
        lz4.Flush();

        return new() {
            Bytes = ms.ToArray(),
        };
    }

    public static LZ4Bytes CreateFromFile(string filePath)
    {
        using var input = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var ms = new MemoryStream();
        using var lz4 = new LZ4Stream(ms, CompressionMode.Compress, LZ4StreamFlags.HighCompression);

        input.CopyTo(lz4);
        lz4.Flush();

        return new() {
            Bytes = ms.ToArray(),
        };
    }

    public static LZ4Bytes CreateFromBytes(byte[] input)
    {
        using var ms = new MemoryStream();
        using var lz4 = new LZ4Stream(ms, CompressionMode.Compress, LZ4StreamFlags.HighCompression);

        lz4.Write(input, 0, input.Length);
        lz4.Flush();

        return new() {
            Bytes = ms.ToArray(),
        };
    }

    public T Decompress<T>()
    {
        using var input = new MemoryStream(Bytes);
        using var lz4 = new LZ4Stream(input, CompressionMode.Decompress);
        using var sr = new StreamReader(lz4, Encoding.UTF8);
        using var jr = new JsonTextReader(sr);

        return _serializer.Deserialize<T>(jr)!;
    }

    public string DecompressToString()
    {
        using var input = new MemoryStream(Bytes);
        using var lz4 = new LZ4Stream(input, CompressionMode.Decompress);
        using var sr = new StreamReader(lz4, Encoding.UTF8);

        return sr.ReadToEnd();
    }

    public byte[] DecompressToBytes()
    {
        using var input = new MemoryStream(Bytes);
        using var lz4 = new LZ4Stream(input, CompressionMode.Decompress);
        using var ms = new MemoryStream();

        lz4.CopyTo(ms);

        return ms.ToArray();
    }

    public void DecompressToStream(Stream output)
    {
        using var input = new MemoryStream(Bytes);
        using var lz4 = new LZ4Stream(input, CompressionMode.Decompress);

        lz4.CopyTo(output);
    }

    public void DecompressToFile(string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        using var fs = File.Create(filePath);

        DecompressToStream(fs);
    }
}