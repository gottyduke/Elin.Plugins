using System;
using System.Collections.Concurrent;
using System.Linq;
using Cwl.API.Attributes;
using ElinTogether.Elements;
using ElinTogether.Net;

namespace ElinTogether.Helper;

internal static class RemoteCardHelper
{
    internal static readonly ConcurrentDictionary<Chara, RemoteCharaNetProfile> RemoteCardNetProfile = [];

    [CwlPreLoad]
    [CwlSceneInitEvent(Scene.Mode.Title, preInit: true)]
    private static void ClearProfiles()
    {
        RemoteCardNetProfile.Clear();
    }

    internal class RemoteCharaNetProfile(Chara chara)
    {
        public bool IsRemotePlayer => NetSession.Instance.CurrentPlayers.Find(s => s.CharaUid == chara.uid) is not null;

        public WeakReference<Thing> RemoteMainHand { get; set; } = new(null!, false);
        public WeakReference<Thing> RemoteOffHand { get; set; } = new(null!, false);

        public GoalRemote GoalDefault => field ??= new();
    }

    extension(Chara chara)
    {
        internal RemoteCharaNetProfile NetProfile => RemoteCardNetProfile.GetOrAdd(chara, chara => new(chara));

        internal bool IsRemotePlayer => NetSession.Instance.CurrentPlayers.Any(n => n.CharaUid == chara.uid);
    }
}