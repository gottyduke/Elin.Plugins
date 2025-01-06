using System.Collections.Generic;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace Glance;

internal class GlanceAPI
{
    private const string GlanceGuid = "dk.elinplugins.modglance";
    private const string GlanceRegistry = "RegisterGlance";
    
    private static Component? _glance;
    private static readonly List<string> _queued = [];
    internal static bool Unavailable { get; private set; }
    
    /// <summary>
    /// Dispatch messages to glance
    /// </summary>
    internal static void Dispatch(object message)
    {
        if (Unavailable) {
            return;
        }

        _queued.Add((string)message);
    }

    /// <summary>
    /// Only call this in <b>OnStartCore</b>, <b>Start</b>, or later
    /// </summary>
    internal static void TryConnect()
    {
        _glance = ModManager.ListPluginObject
            .OfType<BaseUnityPlugin>()
            .FirstOrDefault(p => p.Info.Metadata.GUID == GlanceGuid);
        _glance?.SendMessage(GlanceRegistry);
        
        Unavailable = _glance == null;
    }
    
    /// <summary>
    /// Invoked by glance to retrieve queued messages
    /// </summary>
    private static IEnumerable<string> PopAll()
    {
        var current = _queued.ToList();
        _queued.Clear();
        return current;
    }
}