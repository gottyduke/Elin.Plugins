namespace Cwl.Helper.Extensions;

public static class ParserExt
{
    extension(string unparsed)
    {
        public float AsFloat(float fallback)
        {
            if (!float.TryParse(unparsed, out var result)) {
                result = fallback;
            }

            return result;
        }

        public double AsDouble(double fallback)
        {
            if (!double.TryParse(unparsed, out var result)) {
                result = fallback;
            }

            return result;
        }

        public int AsInt(int fallback)
        {
            if (!int.TryParse(unparsed, out var result)) {
                result = fallback;
            }

            return result;
        }

        public bool AsBool(bool fallback)
        {
            unparsed = unparsed.ToLowerInvariant().Trim();
            return unparsed switch {
                "true" or "1" or "on" or "yes" => true,
                "false" or "0" or "off" or "no" => false,
                _ => fallback,
            };
        }
    }
}