using MessagePack;

namespace ElinTogether.Net;

[MessagePackObject]
public class NetPeerState
{
    [Key(0)]
    public required int Index { get; init; }

    [Key(1)]
    public required ulong PeerUid { get; init; }

    [Key(2)]
    public required string Name { get; init; }

    [Key(3)]
    public required int CharaUid { get; init; }

    [Key(4)]
    public int Speed { get; set; }

    [Key(5)]
    public int LastAct { get; set; }

    [Key(6)]
    public int LastReceivedTick { get; set; } = -1;

    [Key(7)]
    public int LastPingMs { get; set; }

    [Key(8)]
    public float AvgPingMs { get; set; }

    [Key(9)]
    public float ConnectionQualityLocal { get; set; }

    [Key(10)]
    public float ConnectionQualityRemote { get; set; }

    public Chara? FindChara()
    {
        return EClass.pc.party.members.Find(c => c.uid == CharaUid);
    }
}