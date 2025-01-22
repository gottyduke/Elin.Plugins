using System;

namespace Cwl.Helper.String;

public static class Capitalizer
{
    public static ReadOnlySpan<char> Capitalize(this ReadOnlySpan<char> input)
    {
        Span<char> buf = input.ToArray();
        buf[0] = char.ToUpper(buf[0]);
        return buf;
    }

    public static ReadOnlySpan<char> Capitalize(this string input)
    {
        Span<char> buf = input.ToCharArray();
        buf[0] = char.ToUpper(buf[0]);
        return buf;
    }
}