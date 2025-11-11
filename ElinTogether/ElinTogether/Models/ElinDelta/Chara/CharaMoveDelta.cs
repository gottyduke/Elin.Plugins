using ElinTogether.Net;
using ElinTogether.Patches.DeltaEvents;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaMoveDelta : IElinDelta
{
    public enum CharaMoveType : byte
    {
        Walk,
        Force,
    }

    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public int PosX { get; init; }

    [Key(2)]
    public int PosZ { get; init; }

    [Key(3)]
    public CharaMoveType MoveType { get; init; }

    public void Apply(ElinNetBase net)
    {
        // this only happens on game load
        if (EClass.core.game?.activeZone?.map is null) {
            net.Delta.DeferLocal(this);
            return;
        }

        // we do not apply to ourselves
        if (Owner.Find() is not Chara { IsPC: false } chara) {
            return;
        }

        // drop this
        if (chara.currentZone != EClass.pc.currentZone) {
            return;
        }

        if (chara.pos.x == PosX && chara.pos.z == PosZ) {
            return;
        }

        chara.Stub_Move(new(PosX, PosZ), (Card.MoveType)MoveType);

        // relay this move again to other peers
        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }
    }

    public static implicit operator CharaMoveDelta(Chara chara)
    {
        return new() {
            Owner = chara,
            PosX = chara.pos.x,
            PosZ = chara.pos.z,
            MoveType = (CharaMoveType)Card.MoveType.Force,
        };
    }
}