using System.Collections.Generic;
using System.Linq;
using ElinTogether.Helper;
using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class WorldStateSnapshot : EClass
{
    public static readonly List<CharaStateSnapshot> CachedRemoteSnapshots = [];

    [Key(0)]
    public required int ServerTick { get; init; }

    [Key(1)]
    public required int[] GameDate { get; init; }

    [Key(2)]
    public required CharaStateSnapshot[] CharaSnapshots { get; init; }

    [Key(3)]
    public required int GlobalUidNext { get; init; }

    [Key(4)]
    public required int SharedSpeed { get; init; }

    public static WorldStateSnapshot Create(IEnumerable<Chara> excludeSnapshots)
    {
        CachedRemoteSnapshots.Add(CharaStateSnapshot.CreateSelf());

        var charaSnapshots = _map.charas
            .Except([pc, ..excludeSnapshots])
            .Select(CharaStateSnapshot.Create)
            .Concat(CachedRemoteSnapshots)
            .ToArray();

        CachedRemoteSnapshots.Clear();

        return new() {
            ServerTick = NetSession.Instance.Tick,
            GameDate = game.world.date.raw,
            CharaSnapshots = charaSnapshots,
            GlobalUidNext = game.cards.uidNext,
            SharedSpeed = NetSession.Instance.SharedSpeed,
        };
    }

    public void ApplyReconciliation()
    {
        // 1
        EClass.world.date.raw = GameDate;

        // 2
        foreach (var snapshot in CharaSnapshots) {
            if (snapshot.Owner.Find() is Chara chara) {
                snapshot.ApplyReconciliation(chara);
            }
        }

        // 3
        EClass.game.cards.uidNext = GlobalUidNext;

        // 4
        NetSession.Instance.SharedSpeed = SharedSpeed;
    }
}