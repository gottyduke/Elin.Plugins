using System.Text.RegularExpressions;

namespace ACS.API;

public class AcsClip(string name, int interval, int begin, int end)
{
    private static readonly Regex _acsFormat = new(
        @"^_acs_(?<name>[^#]+)#(?<interval>\d+)_(?<begin>\d+)-(?<end>\d+)$",
        RegexOptions.Compiled);
    public readonly int Begin = begin;
    public readonly int End = end;
    public readonly int Interval = interval;

    public readonly string Name = name;

    public int Length => End - Begin + 1;

    public static AcsClip? CreateFromFormat(string format)
    {
        var match = _acsFormat.Match(format);
        if (!match.Success) {
            return null;
        }

        if (!int.TryParse(match.Groups["interval"].Value, out var interval)) {
            interval = 66;
        }

        if (!int.TryParse(match.Groups["begin"].Value, out var begin)) {
            begin = 0;
        }

        if (!int.TryParse(match.Groups["end"].Value, out var end)) {
            end = begin;
        }

        return new(match.Groups["name"].Value, interval, begin, end);
    }
}