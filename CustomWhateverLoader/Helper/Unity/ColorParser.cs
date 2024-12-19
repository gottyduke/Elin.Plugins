using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Cwl.Helper.Unity;

public static class ColorParser
{
    public static Color ParseColorVector(this string[] vector)
    {
        if (vector.Length < 3) {
            return Color.clear;
        }

        var parsed = vector.Select(float.Parse).ToList();
        if (parsed.Count == 3) {
            parsed.Add(1f);
        }

        return new(parsed[0], parsed[1], parsed[2], parsed[3]);
    }

    public static Color ParseColorHex(this string hex)
    {
        var bits = hex.Trim().Replace("0x", "").Replace("#", "").ToCharArray();
        Array.Resize(ref bits, 8);

        var r = byte.Parse(hex[..2], NumberStyles.HexNumber);
        var g = byte.Parse(hex[2..4], NumberStyles.HexNumber);
        var b = byte.Parse(hex[4..6], NumberStyles.HexNumber);
        var a = byte.Parse(hex[6..8], NumberStyles.HexNumber);

        const float byteMax = byte.MaxValue;
        return new(r / byteMax, g / byteMax, b / byteMax, a / byteMax);
    }
}