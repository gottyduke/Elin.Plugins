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
    public required int Hp { get; set; }

    // player provided
    [Key(4)]
    public PlayerCharaStateSnapshot? State { get; set; }

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

        RemoteCard? heldMainHand = player.currentHotItem.RenderThing
                                   ?? (hideWeapon ? pc.held : pc.body.slotMainHand?.thing);
        RemoteCard? heldOffHand = core.config.game.showOffhand
            ? pc.body.slotOffHand?.thing
            : pc.held;

        // held item override
        if (pc.held is not null) {
            heldMainHand = heldOffHand = pc.held;
        }

        return new() {
            Owner = pc,
            Pos = pc.pos,
            CurrentZoneUid = pc.currentZone.uid,
            Hp = pc.hp,
            State = new() {
                LastAct = SourceValidation.ActToIdMapping[pc.ai.GetType()],
                LastReceivedTick = NetSession.Instance.Tick,
                Speed = pc.Stub_get_Speed(),
                HeldMainHand = heldMainHand,
                HeldOffHand = heldOffHand,
                Dir = pc.dir,
            },
        };
    }

    /// <summary>
    ///     This only applies to client characters
    ///     We assume host is always right
    /// </summary>
    public void ApplyReconciliation(Chara? remoteChara = null)
    {
        var chara = remoteChara ?? Owner.Find() as Chara;
        if (chara is null) {
            return;
        }

        // this is received from host side
        if (remoteChara is null) {
            chara.hp = Hp;
        }

        // one more check for lingering death events
        if (chara.isDead && chara.pos != Pos) {
            chara.Stub_Revive(Pos, true);
        }

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

        // this is from remote players
        if (State is null) {
            return;
        }

        // update tool visual
        chara.NetProfile.RemoteMainHand = new(State.HeldMainHand, false);
        chara.NetProfile.RemoteOffHand = new(State.HeldOffHand, false);

        // apply held visual
        if (State.HeldMainHand?.Find() is { } mainHand &&
            State.HeldOffHand?.Find() is { } offHand &&
            mainHand == offHand) {
            chara.HoldCard(mainHand);
        }

        // apply direction
        if (chara.dir != State.Dir) {
            chara.SetDir(State.Dir);
        }
    }
}