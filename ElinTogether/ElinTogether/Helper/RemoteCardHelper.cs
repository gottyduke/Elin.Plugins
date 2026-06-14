using System;
using System.Collections.Concurrent;
using System.Linq;
using ElinTogether.Elements;
using ElinTogether.Net;

namespace ElinTogether.Helper;

internal static class RemoteCardHelper
{
    internal static readonly ConcurrentDictionary<Chara, RemoteCharaNetProfile> RemoteCardNetProfile = [];

    [ElinPreLoad]
    private static void ClearProfiles(GameIOContext context)
    {
        RemoteCardNetProfile.Clear();
    }

    [ElinPreSceneInit]
    private static void ClearProfiles(Scene.Mode mode)
    {
        if (mode == Scene.Mode.Title) {
            RemoteCardNetProfile.Clear();
        }
    }

    internal class RemoteCharaNetProfile
    {
        public WeakReference<Thing> RemoteMainHand { get; set; } = new(null!, false);
        public WeakReference<Thing> RemoteOffHand { get; set; } = new(null!, false);

        public GoalRemote GoalDefault => field ??= new();
    }

    extension(Chara chara)
    {
        internal RemoteCharaNetProfile NetProfile => RemoteCardNetProfile.GetOrAdd(chara, chara => new());

        internal bool IsRemotePlayer
        {
            get {
                return NetSession.Instance.Connection switch {
                    ElinNetHost => chara.ai is GoalRemote,
                    ElinNetClient => !chara.IsPC && NetSession.Instance.CurrentPlayers.Any(n => n.CharaUid == chara.uid),
                    _ => false,
                };
            }
        }

        internal bool IsPlayer => chara.IsPC || chara.IsRemotePlayer;
    }
}