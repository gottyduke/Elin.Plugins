namespace Cwl.Helper.Extensions;

public static class CardExt
{
    public static bool HasFlagDefined(this Card owner, string flag)
    {
        return owner.sourceCard.tag.Contains($"addFlag_{flag}");
    }

    public static int GetFlagValue(this Card owner, string flag)
    {
        var key = flag.GetHashCode();
        if (!owner.mapInt.TryGetValue(key, out var value)) {
            value = owner.mapInt[key] = owner.HasFlagDefined(flag) ? 1 : 0;
        }

        return value;
    }
}