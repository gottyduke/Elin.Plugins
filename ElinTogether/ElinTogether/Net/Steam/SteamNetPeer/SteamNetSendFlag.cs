using System;

namespace ElinTogether.Net.Steam;

[Flags]
public enum SteamNetSendFlag
{
    Unreliable = 0,
    NoNagle = 1 << 0,
    NoDelay = 1 << 2,
    Reliable = 1 << 3,

    // alias
    UnreliableNoNagle = Unreliable | NoNagle,
    UnreliableNoDelay = Unreliable | NoDelay | NoNagle,
}