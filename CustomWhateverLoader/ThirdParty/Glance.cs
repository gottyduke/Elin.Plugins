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

    internal static IEnumerable<string> PopAll()
    {
        var current = _queued.ToList();
        _queued.Clear();
        return current;
    }

    internal static void TryConnect()
    {
        _glance = TypeQualifier.Plugins?.FirstOrDefault(p => p.Info.Metadata.GUID == GlanceGuid);
        _glance?.SendMessage("RegisterGlance");
        _unavailable = _glance == null;
    }
}