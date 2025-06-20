using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper;
using UnityEngine;

namespace Cwl.ThirdParty;

internal class Glance
{
    private const string GlanceGuid = "dk.elinplugins.modglance";
    private static Component? _glance;
    private static bool _unavailable;

    private static readonly List<string> _queued = [];

    internal static void Dispatch(object message)
    {
        if (_unavailable) {
            return;
        }

        _queued.Add((string)message);
    }

    internal static void TryConnect()
    {
        _glance = TypeQualifier.Plugins?.FirstOrDefault(p => p.Info.Metadata.GUID == GlanceGuid);
        _glance?.SendMessage("RegisterGlance", "CWL");
        _unavailable = _glance == null;
    }

    private static ArraySegment<string> PopAll()
    {
        var current = _queued.ToArray();
        _queued.Clear();
        return current;
    }
}