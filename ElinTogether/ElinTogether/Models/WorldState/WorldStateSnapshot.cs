using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class WorldStateSnapshot : EClass
{
    public static readonly List<CharaStateSnapshot> CachedRemoteSnapshots = [];

    [Key(0)]
    public required int ServerTick { get; init; }

    [Key(1)]
    public required ImmutableArray<int> GameDate { get; init; }

    [Key(2)]
    public required ImmutableArray<CharaStateSnapshot> CharaSnapshots { get; init; }

    [Key(3)]
    public required int GlobalUidNext { get; init; }

    [Key(4)]
    public required int SharedSpeed { get; init; }

    public static WorldStateSnapshot Create()
    {
        CachedRemoteSnapshots.Add(CharaStateSnapshot.CreateSelf());

        var snapshots = new Dictionary<int, CharaStateSnapshot>();
        foreach (var chara in _map.charas) {
            snapshots[chara.uid] = CharaStateSnapshot.Create(chara);
        }

        // attach remote state to client characters
        foreach (var remoteSnapshot in CachedRemoteSnapshots) {
            if (snapshots.TryGetValue(remoteSnapshot.Owner.Uid, out var snapshot)) {
                snapshot.State = remoteSnapshot.State;
            }
        }

        CachedRemoteSnapshots.Clear();

        return new() {
            ServerTick = NetSession.Instance.Tick,
            GameDate = [..game.world.date.raw],
            CharaSnapshots = [..snapshots.Values],
            GlobalUidNext = game.cards.uidNext,
            SharedSpeed = NetSession.Instance.SharedSpeed,
        };
    }

    public void ApplyReconciliation()
    {
        // 1
        world.date.raw = GameDate.ToArray();

        // 2
        foreach (var snapshot in CharaSnapshots) {
            snapshot.ApplyReconciliation();
        }

        // 3
        game.cards.uidNext = GlobalUidNext;

        // 4
        NetSession.Instance.SharedSpeed = SharedSpeed;
    }
}