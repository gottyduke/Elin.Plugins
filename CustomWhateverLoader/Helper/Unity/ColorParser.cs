using System;
using System.Globalization;
using System.Linq;
using Cwl.LangMod;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
}