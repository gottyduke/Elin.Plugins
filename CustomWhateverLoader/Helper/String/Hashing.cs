using System.Linq;

namespace Cwl.Helper.String;

public static class Hashing
{
    private const uint FnvPrime32 = 0x1000193;
    private const uint FnvOffset32 = 0x811c9dc5;

    // roslyn uses this for switch hashing
    public static uint Fnv1A(this string label)
    {
        return label.Aggregate(FnvOffset32, (hash, c) => (c ^ hash) * FnvPrime32);
    }

    public static string UniqueString(this Playlist mold)
    {
        var list = mold.ToInts();
        return $"{mold.name}_{string.Join('/', list)}";
    }
}