using ElinTogether.Helper;
using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class CharaStateSnapshot : EClass
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public required Position Pos { get; init; }

    [Key(2)]
    public required int CurrentZoneUid { get; init; }

    [Key(3)]
    public required int Hp { get; init; }

    // client provided

    [Key(4)]
    public int LastAct { get; init; }

    [Key(5)]
    public int LastReceivedTick { get; init; }

    [Key(6)]
    public int Speed { get; init; }

    [Key(7)]
    public RemoteCard? HeldMainHand { get; init; }

    [Key(8)]
    public RemoteCard? HeldOffHand { get; init; }

    [Key(9)]
    public int Dir { get; init; }

    public static CharaStateSnapshot Create(Chara chara)
    {
        return new() {
            Owner = chara,
            Pos = chara.pos,
            CurrentZoneUid = chara.currentZone.uid,
            Hp = chara.hp,
        };
    }

    public static CharaStateSnapshot CreateSelf()
    {
        var hideWeapon = pc.combatCount <= 0 && core.config.game.hideWeapons;

        RemoteCard? heldMainHand = player.currentHotItem.RenderThing ??
                                   (hideWeapon ? pc.held : pc.body.slotMainHand?.thing);
        RemoteCard? heldOffHand = core.config.game.showOffhand ? pc.body.slotOffHand?.thing : pc.held;

        return new() {
            Owner = pc,
            Pos = pc.pos,
            CurrentZoneUid = pc.currentZone.uid,
            Hp = pc.hp,
            LastAct = SourceValidation.ActToIdMapping[pc.ai.GetType()],
            LastReceivedTick = NetSession.Instance.Tick,
            Speed = pc.Stub_get_Speed(),
            HeldMainHand = heldMainHand,
            HeldOffHand = heldOffHand,
            Dir = pc.dir,
        };
    }

    /// <summary>
    ///     This only applies to client characters
    ///     We assume host is always right
    /// </summary>
    public void ApplyReconciliation(Chara? chara)
    {
        chara ??= Owner.Find() as Chara;
        if (chara is null) {
            return;
        }

        chara.hp = Hp;

        // fixes should only be applied to other remote charas
        if (chara.IsPC) {
            return;
        }

        if (chara.currentZone?.uid != CurrentZoneUid) {
            // chara hasn't been brought to the same map yet
            if (CurrentZoneUid == NetSession.Instance.CurrentZone?.uid) {
                NetSession.Instance.CurrentZone.AddCard(chara, Pos);
            }

            return;
        }

        // somehow we are riding it
        if (chara.host is null) {
            // only apply position fix if on the same map
            if (NetSession.Instance.CurrentZone?.map is not null && chara.pos != Pos) {
                chara.Stub_Move(Pos, Card.MoveType.Force);
            }
        }

        // update tool visual
        chara.NetProfile.RemoteMainHand = new(HeldMainHand, false);
        chara.NetProfile.RemoteOffHand = new(HeldOffHand, false);

        // apply held visual
        if (HeldMainHand?.Find() is { } mainHand && HeldOffHand?.Find() is { } offHand) {
            if (mainHand == offHand) {
                chara.HoldCard(mainHand);
            }
        }

        // apply direction
        if (chara.dir != Dir) {
            chara.SetDir(Dir);
        }

        // one more check for lingering death events
        if (chara.isDead && chara.pos != Pos) {
            chara.Stub_Revive(Pos, msg: true);
        }
    }
}