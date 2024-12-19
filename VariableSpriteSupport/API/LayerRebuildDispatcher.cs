using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace VSS.API;

public class LayerRebuildDispatcher
{
    private static readonly List<Action<PCC.Layer, Texture2D, int>> _dispatchers = [];

    public static void AddDispatcher(Action<PCC.Layer, Texture2D, int> dispatcher)
    {
        _dispatchers.Add(SafeInvoke);
        return;

        void SafeInvoke(PCC.Layer layer, Texture2D tex, int layerIndex)
        {
            try {
                dispatcher(layer, tex, layerIndex);
            } catch {
                // noexcept
            }
        }
    }

    internal static void Dispatch(PCC.Layer layer, Texture2D tex, int layerIndex)
    {
        _dispatchers.Do(d => d(layer, tex, layerIndex));
    }
}