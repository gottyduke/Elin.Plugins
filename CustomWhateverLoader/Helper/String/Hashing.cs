using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Cwl.Helper.String;

public static class Hashing
{
    private const uint FnvPrime32 = 0x1000193;
    private const uint FnvOffset32 = 0x811c9dc5;

    [field: AllowNull]
    private static SHA256 Sha256 => field ??= SHA256.Create();

    public static string UniqueString(this Playlist mold)
    {
        var list = mold.ToInts();
        return $"{mold.name}_{string.Join('/', list)}";
    }

    extension(string input)
    {
        // roslyn uses this for switch hashing
        public uint Fnv1A()
        {
            return input.Aggregate(FnvOffset32, (hash, c) => (c ^ hash) * FnvPrime32);
        }

        public string GetSha256Code()
        {
            var hash = input.GetSha256Hash();

            using var sb = StringBuilderPool.Get();
            foreach (var @byte in hash) {
                sb.Append(@byte.ToString("x2"));
            }

            return sb.ToString();
        }

        public Span<byte> GetSha256Hash()
        {
            return Sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        }
    }

    extension(FileStream fs)
    {
        public string GetSha256Code()
        {
            var pos = fs.Position;
            fs.Seek(0, SeekOrigin.Begin);

            var hash = Sha256.ComputeHash(fs);
            fs.Position = pos;

            using var sb = StringBuilderPool.Get();
            foreach (var @byte in hash) {
                sb.Append(@byte.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}