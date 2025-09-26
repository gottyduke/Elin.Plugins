using System.Collections.Generic;
using UnityEngine;

namespace Cwl.Helper.Unity;

public static class ColorParser
{
    private static readonly Dictionary<string, Color> _colors = [];

    extension(string colorString)
    {
        public Color ToColorEx()
        {
            if (!_colors.TryGetValue(colorString, out var color)) {
                color = _colors[colorString] = colorString.Replace("#", "").Replace("0x", "").ToColor();
            }

            return color;
        }
    }
}