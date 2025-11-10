using ElinTogether.Net;
using ElinTogether.Patches.DeltaEvents;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaMoveZoneDelta : IElinDelta
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public required string ZoneFullName { get; init; }

    [Key(2)]
    public required int ZoneUid { get; init; }

    [Key(4)]
    public int PosX { get; init; }

    [Key(5)]
    public int PosZ { get; init; }

    public void Apply(ElinNetBase net)
    {
        if (net.IsHost) {
            // reject every single move zone delta from clients
            return;
        }

        if (EClass.game.activeZone?.map is null) {
            // defer for map loading
            net.Delta.DeferLocal(this);
            return;
        }

        if (Owner.Find() is not Chara chara) {
            return;
        }

        var remoteZone = EClass.game.spatials.Find(ZoneUid);
        if (remoteZone is null || (chara.IsPC && NetSession.Instance.CurrentZone != remoteZone)) {
            // defer to catch up
            net.Delta.DeferLocal(this);
            return;
        }

        if (chara.currentZone == remoteZone) {
            // update position only
            net.Delta.AddLocal(new CharaMoveDelta {
                Owner = Owner,
                PosX = PosX,
                PosZ = PosZ,
                MoveType = (CharaMoveDelta.CharaMoveType)Card.MoveType.Force,
            });
            return;
        }

        // move zone with exact transition
        chara.Stub_MoveZone3(remoteZone, new() {
            state = ZoneTransition.EnterState.Exact,
            x = PosX,
            z = PosZ,
        });
    }
}