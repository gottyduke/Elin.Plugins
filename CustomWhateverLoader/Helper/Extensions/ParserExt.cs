namespace Cwl.Helper.Extensions;

public static class ParserExt
{
    public static float AsFloat(this string unparsed, float fallback)
    {
        if (!float.TryParse(unparsed, out var result)) {
            result = fallback;
        }

        return result;
    }

    public static double AsDouble(this string unparsed, double fallback)
    {
        if (!double.TryParse(unparsed, out var result)) {
            result = fallback;
        }

        return result;
    }

    public static int AsInt(this string unparsed, int fallback)
    {
        if (!int.TryParse(unparsed, out var result)) {
            result = fallback;
        }

        return result;
    }

    public static bool AsBool(this string unparsed, bool fallback)
    {
        unparsed = unparsed.ToLowerInvariant().Trim();
        return unparsed switch {
            "true" or "1" or "on" or "yes" => true,
            "false" or "0" or "off" or "no" => false,
            _ => fallback,
        };
    }
}