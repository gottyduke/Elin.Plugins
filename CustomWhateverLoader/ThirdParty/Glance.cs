using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Cwl.Loader;
using UnityEngine;

namespace Cwl.ThirdParty;

public class Glance
{
    private const string GlanceGuid = "dk.elinplugins.modglance";
    private static Component? _glance;
    private static bool _unavailable;

    private static readonly List<string> _queued = [];

    public static void Dispatch(object message)
    {
        if (_unavailable) {
            return;
        }

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

        if (_glance == null) {
            _unavailable = true;
            return;
        }

        _glance.SendMessage("Register", CwlMod.Instance);
    }
}