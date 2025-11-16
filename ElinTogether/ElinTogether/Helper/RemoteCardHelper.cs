using System;
using System.Collections.Concurrent;
using System.Linq;
using Cwl.API.Attributes;
using ElinTogether.Net;

namespace ElinTogether.Helper;

internal static class RemoteCardHelper
{
    internal static readonly ConcurrentDictionary<Chara, RemoteCharaNetProfile> _remoteCardNetProfile = [];

    [CwlPreLoad]
    [CwlSceneInitEvent(Scene.Mode.Title)]
    private static void ClearProfiles()
    {
        _remoteCardNetProfile.Clear();
    }

    internal class RemoteCharaNetProfile(Chara chara)
    {
        public bool IsPlayer => NetSession.Instance.CurrentPlayers.FirstOrDefault(s => s.CharaUid == chara.uid) is not null;

        public WeakReference<Thing> RemoteMainHand { get; set; } = new(null!, false);
        public WeakReference<Thing> RemoteOffHand { get; set; } = new(null!, false);
    }

    extension(Chara chara)
    {
        internal RemoteCharaNetProfile NetProfile => _remoteCardNetProfile.GetOrAdd(chara, chara => new(chara));
    }
}