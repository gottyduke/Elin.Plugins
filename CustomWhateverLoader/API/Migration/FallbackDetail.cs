using System;
using System.Collections.Generic;

namespace Cwl.API.Migration;

public static class FallbackDetail
{
    public static readonly Dictionary<Type, object> Fallbacks = new() {
        [typeof(string)] = "",
        [typeof(string[])] = Array.Empty<string>(),
        [typeof(int)] = 0,
        [typeof(int[])] = Array.Empty<int>(),
        [typeof(bool)] = false,
        [typeof(float)] = 0f,
        [typeof(float[])] = Array.Empty<float>(),
    };
}