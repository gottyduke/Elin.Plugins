using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class CharaSnapshot
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public int Hp { get; init; }

    [Key(2)]
    public required Position Pos { get; init; }

    [Key(3)]
    public required int CurrentZoneUid { get; init; }

    public static CharaSnapshot Create(Chara chara)
    {
        return new() {
            Owner = chara,
            Hp = chara.hp,
            Pos = chara.pos,
            CurrentZoneUid = chara.currentZone.uid,
        };
    }

    /// <summary>
    ///     This only applies to client side
    ///     We assume host is always right
    /// </summary>
    public void ApplyReconciliation()
    {
        if (Owner.Find() is not Chara chara) {
            return;
        }

        chara.hp = Hp;

        // we do not roll back as clients
        // that would cause a lot of de-syncs
        if (chara.IsPC) {
            return;
        }

        // only apply position fix if on the same map
        // if chara hasn't been moved to new remote zone, do it
        // TODO separate this logic from reconciliation
        if (chara.currentZone.uid != CurrentZoneUid) {
            if (CurrentZoneUid == NetSession.Instance.CurrentZone?.uid) {
                chara.MoveZone(NetSession.Instance.CurrentZone);
            }
            return;
        }

        if (Point.map == NetSession.Instance.CurrentZone?.map && chara.pos != Pos) {
            chara.Stub_Move(Pos, Card.MoveType.Force);
        }
    }
}