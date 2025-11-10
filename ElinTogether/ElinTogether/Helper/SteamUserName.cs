using System;
using System.Collections.Generic;
using Steamworks;

namespace ElinTogether.Helper;

internal class SteamUserName
{
    private static bool _allocated;
    private static readonly Dictionary<ulong, Action<string>> _deferredPins = [];

    public static void PinUserName(ulong steamId, Action<string> setter)
    {
        if (!_allocated) {
            SteamCallback<PersonaStateChange_t>.Add(HandlePersonaNameChange);
            _allocated = true;
        }

        var needsUpdate = SteamFriends.RequestUserInformation((CSteamID)steamId, true);
        if (!needsUpdate) {
            setter(SteamFriends.GetFriendPersonaName((CSteamID)steamId));
        } else {
            _deferredPins[steamId] = setter;
        }
    }

    private static void HandlePersonaNameChange(PersonaStateChange_t state)
    {
        var steamId = state.m_ulSteamID;
        if (_deferredPins.Remove(steamId, out var setter)) {
            setter(SteamFriends.GetFriendPersonaName((CSteamID)state.m_ulSteamID));
        }
    }
}