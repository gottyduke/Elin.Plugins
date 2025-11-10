using Steamworks;

namespace ElinTogether.Helper;

internal class PlayerUidMaker
{
    internal const ulong KusoUid = 0x114_514UL & 0x19_19_810UL;

    internal static ulong MakeSteamUid()
    {
        return (ulong)SteamUser.GetSteamID();
    }
}