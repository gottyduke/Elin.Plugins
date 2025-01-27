using System;
using System.Collections.Generic;
using Cwl.LangMod;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cwl.Patches.Relocation;

[HarmonyPatch]
public class LoadResourcesPatch
{
    public delegate bool RelocationHandler<T>(string path, ref T loaded);

    private static readonly Dictionary<Type, List<RelocationHandler<Object>>> _handlers = [];

    public static void AddHandler<T>(RelocationHandler<Object> handler) where T : Object
    {
        _handlers.TryAdd(typeof(T), []);
        _handlers[typeof(T)].Add(SafeLoad);
        return;

        bool SafeLoad(string path, ref Object loaded)
        {
            try {
                return handler(path, ref loaded);
            } catch (Exception ex) {
                CwlMod.WarnWithPopup<T>("cwl_error_failure".Loc(ex.Message), ex);
                // noexcept
            }

            return false;
        }
    }

    [SwallowExceptions]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Resources), nameof(Resources.Load), typeof(string), typeof(Type))]
    internal static bool OnRelocateResource(string path, Type systemTypeInstance, ref Object __result)
    {
        if (!_handlers.TryGetValue(systemTypeInstance, out var handlers)) {
            return true;
        }

        foreach (var handler in handlers) {
            if (handler(path, ref __result)) {
                return false;
            }
        }

        return true;
    }
}