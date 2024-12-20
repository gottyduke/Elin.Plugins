﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cwl.LangMod;
using Cwl.Loader;
using UnityEngine;

namespace Cwl.Helper;

internal class ExecutionAnalysis
{
    private static readonly Dictionary<MethodBase, List<TimeSpan>> _cached = [];

    internal static void DispatchAnalysis()
    {
        if (_cached.Count == 0) {
            return;
        }

        CwlMod.Log("cwl_log_execution_analysis".Loc());

        var methodNameWidth = _cached.Keys.Max(mi => (mi.DeclaringType?.Name.Length ?? 0) + mi.Name.Length);
        var total = 0d;

        foreach (var (callstack, counted) in _cached) {
            var method = $"{callstack.DeclaringType?.Name}.{callstack.Name}";
            method = method.PadRight(methodNameWidth + 1);

            var plural = counted.Count == 1 ? " " : "s";
            var elapsed = counted.Sum(e => e.TotalMilliseconds);
            total += elapsed;

            Debug.Log("cwl_log_execution_detail".Loc(method, counted.Count, plural, elapsed));
        }

        Debug.Log("cwl_log_execution_tally".Loc(total));
        _cached.Clear();
    }

    internal static class MethodTimeLogger
    {
        public static void Log(MethodBase methodBase, TimeSpan elapsed, string message)
        {
            _cached.TryAdd(methodBase, []);
            _cached[methodBase].Add(elapsed);
        }
    }
}