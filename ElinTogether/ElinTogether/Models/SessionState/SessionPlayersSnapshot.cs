using System.Collections.Immutable;
using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

/// <summary>
///     Net packet: Host -> Client
/// </summary>
[MessagePackObject]
public class SessionPlayersSnapshot
{
    [Key(0)]
    public required ImmutableArray<NetPeerState> Current { get; init; }

    public static SessionPlayersSnapshot Create()
    {
        return new() {
            Current = [..NetSession.Instance.CurrentPlayers],
        };
    }

    public void Apply()
    {
        NetSession.Instance.CurrentPlayers.Clear();
        NetSession.Instance.CurrentPlayers.AddRange(Current);
    }
}