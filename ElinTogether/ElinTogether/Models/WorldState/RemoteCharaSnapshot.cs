using ElinTogether.Helper;
using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models;

/// <summary>
///     A simpler packet from client to host
/// </summary>
[MessagePackObject]
public class RemoteCharaSnapshot
{
    [Key(0)]
    public required ushort LastAct { get; init; }

    [Key(1)]
    public required Position Pos { get; init; }

    [Key(2)]
    public required int Speed { get; init; }

    [Key(3)]
    public required int ZoneUid { get; init; }

    [Key(4)]
    public required int LastReceivedTick { get; init; }

    public static RemoteCharaSnapshot Create()
    {
        var pc = EClass.pc;
        return new() {
            LastAct = SourceValidation.ActToIdMapping[pc.ai.GetType()],
            Pos = pc.pos,
            Speed = pc.Stub_get_Speed(),
            ZoneUid = pc.currentZone.uid,
            LastReceivedTick = NetSession.Instance.Tick,
        };
    }
}