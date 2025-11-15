using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaMoveDelta : ElinDeltaBase
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public required Position Pos { get; init; }

    [Key(2)]
    public Card.MoveType MoveType { get; init; }

    public static implicit operator CharaMoveDelta(Chara chara)
    {
        return new() {
            Owner = chara,
            Pos = chara.pos,
            MoveType = Card.MoveType.Force,
        };
    }

    public override void Apply(ElinNetBase net)
    {
        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

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
        if (chara.currentZone != NetSession.Instance.CurrentZone) {
            return;
        }

        if (chara.pos != Pos) {
            chara.Stub_Move(Pos, MoveType);
        }
    }
}