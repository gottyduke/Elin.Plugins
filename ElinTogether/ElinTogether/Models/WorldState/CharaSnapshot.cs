using ElinTogether.Patches.DeltaEvents;
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
    public int PosX { get; init; }

    [Key(3)]
    public int PosZ { get; init; }

    [Key(4)]
    public int ZoneUid { get; init; }

    public static CharaSnapshot Create(Chara chara)
    {
        return new() {
            Owner = chara,
            Hp = chara.hp,
            PosX = chara.pos.x,
            PosZ = chara.pos.z,
            ZoneUid = chara.currentZone.uid,
        };
    }

    /// <summary>
    ///     This only corrects on client side
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
        if (ZoneUid != EClass._zone.uid) {
            return;
        }

        if (chara.pos.x != PosX || chara.pos.z != PosZ) {
            chara.Stub_Move(new(PosX, PosZ), Card.MoveType.Force);
        }
    }
}