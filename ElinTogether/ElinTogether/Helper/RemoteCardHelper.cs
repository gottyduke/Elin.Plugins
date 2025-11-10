using System.Collections.Generic;
using ElinTogether.Net;
using ElinTogether.Patches.DeltaEvents;

namespace ElinTogether.Helper;

internal static class RemoteCardHelper
{
    public static Chara? Find(int uid)
    {
        return EClass.game?.activeZone?.map?.FindChara(uid) ??
               EClass.game?.cards.globalCharas.GetValueOrDefault(uid) ??
               CardGenEvent.TryPop(uid) as Chara;
    }

    public static Thing? FindThing(int uid)
    {
        return EClass.game?.activeZone?.map?.FindThing(uid) ??
               CardGenEvent.TryPop(uid) as Thing;
    }

    public static Card? FindCard(int uid)
    {
        return EClass.game?.activeZone?.map?.FindChara(uid) ??
               EClass.game?.cards.globalCharas.GetValueOrDefault(uid) ??
               CardGenEvent.TryPop(uid);
    }

    extension(Chara chara)
    {
        public bool IsRemoteChara =>
            NetSession.Instance.Connection switch {
                null => false,
                ElinNetHost { IsConnected: true } host => host.ActiveRemoteCharas.Contains(chara),
                ElinNetClient { IsConnected: true } => !chara.IsPC,
                _ => false,
            };
    }
}