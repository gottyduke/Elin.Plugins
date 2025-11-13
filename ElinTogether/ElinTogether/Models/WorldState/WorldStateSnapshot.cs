using System.Collections.Generic;
using System.Linq;
using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class WorldStateSnapshot
{
    [Key(0)]
    public required int ServerTick { get; init; }

    [Key(1)]
    public required int[] GameDate { get; init; }

    [Key(2)]
    public required IEnumerable<CharaSnapshot> CharaSnapshots { get; init; }

    [Key(3)]
    public required int GlobalUidNext { get; init; }

    [Key(4)]
    public required int SharedSpeed { get; init; }

    public static WorldStateSnapshot Create(IEnumerable<Chara> excludeSnapshots)
    {
        return new() {
            ServerTick = NetSession.Instance.Tick,
            GameDate = EClass.game.world.date.raw,
            CharaSnapshots = EClass._map.charas
                .Except(excludeSnapshots)
                .Select(CharaSnapshot.Create),
            GlobalUidNext = EClass.game.cards.uidNext,
            SharedSpeed = NetSession.Instance.SharedSpeed,
        };
    }

    public void ApplyReconciliation()
    {
        // 1
        EClass.world.date.raw = GameDate;

        // 2
        foreach (var chara in CharaSnapshots) {
            chara.ApplyReconciliation();
        }

        // 3
        EClass.game.cards.uidNext = GlobalUidNext;

        // 4
        NetSession.Instance.SharedSpeed = SharedSpeed;
    }
}