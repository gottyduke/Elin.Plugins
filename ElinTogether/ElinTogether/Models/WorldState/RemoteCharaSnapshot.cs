using ElinTogether.Helper;
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
    public required byte LastAct { get; init; }

    [Key(1)]
    public required int PosX { get; init; }

    [Key(2)]
    public required int PosZ { get; init; }

    [Key(3)]
    public required int Speed { get; init; }

    [Key(4)]
    public required int ZoneUid { get; init; }

    public static RemoteCharaSnapshot Create()
    {
        var pc = EClass.pc;
        return new() {
            LastAct = SourceValidation.AIActToByteMapping[pc.ai.GetType()],
            PosX = pc.pos.x,
            PosZ = pc.pos.z,
            Speed = pc.Stub_get_Speed(),
            ZoneUid = pc.currentZone.uid,
        };
    }
}