using System.Collections.Generic;
using System.Linq;
using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class WorldStateSnapshot
{
    [Key(0)]
    public required uint ServerTick { get; init; }

    [Key(1)]
    public required int[] GameDate { get; init; }

    [Key(2)]
    public required IEnumerable<CharaSnapshot> CharaSnapshots { get; init; }

    [Key(3)]
    public required int GlobalUidNext { get; init; }

    public static WorldStateSnapshot Create(IEnumerable<Chara> excludeSnapshots)
    {
        return new() {
            ServerTick = NetSession.Instance.Tick,
            GameDate = EClass.game.world.date.raw,
            CharaSnapshots = EClass._map.charas
                .Except(excludeSnapshots)
                .Select(CharaSnapshot.Create),
            GlobalUidNext = EClass.game.cards.uidNext,
        };
    }
}