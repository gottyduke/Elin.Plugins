using System;

namespace ElinTogether.Net.Steam;

[Flags]
public enum SteamNetLobbyMemberState
{
    Entered = 1 << 0,
    Left = 1 << 1,
    Disconnected = 1 << 2,
    Kicked = 1 << 3,
    Banned = 1 << 4,
}