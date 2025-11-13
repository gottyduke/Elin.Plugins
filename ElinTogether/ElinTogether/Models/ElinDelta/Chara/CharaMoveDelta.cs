using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaMoveDelta : ElinDeltaBase
{
    public enum CharaMoveType : byte
    {
        Walk,
        Force,
    }

    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public required Position Pos { get; init; }

    [Key(2)]
    public CharaMoveType MoveType { get; init; }

    public override void Apply(ElinNetBase net)
    {
        // this only happens on game load
        if (core.game?.activeZone?.map is null) {
            net.Delta.DeferLocal(this);
            return;
        }

        // we do not apply to ourselves
        if (Owner.Find() is not Chara { IsPC: false } chara) {
            return;
        }

        // drop this
        if (chara.currentZone != pc.currentZone) {
            return;
        }

        if (chara.pos == Pos) {
            return;
        }

        chara._Move(Pos, (Card.MoveType)MoveType);
    }

    public static implicit operator CharaMoveDelta(Chara chara)
    {
        return new() {
            Owner = chara,
            Pos = chara.pos,
            MoveType = (CharaMoveType)Card.MoveType.Force,
        };
    }
}