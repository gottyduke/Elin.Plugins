using System.Collections.Generic;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace Cwl;

public class Glance
{
    private const string GlanceGuid = "dk.elinplugins.modglance";
    private static Component? _glance;

    private static readonly List<string> _queued = [];

    public static void Dispatch(object message)
    {
        _queued.Add((string)message);
    }

    public static IEnumerable<string> PopAll()
    {
        var current = _queued.ToList();
        _queued.Clear();
        return current;
    }

    public static void TryConnect()
    {
        var loaded = Resources.FindObjectsOfTypeAll<BaseUnityPlugin>();
        _glance = loaded.FirstOrDefault(p => p.Info.Metadata.GUID == GlanceGuid);
        _glance?.SendMessage("Register", CwlMod.Instance);
    }
}