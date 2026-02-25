using ElinTogether.Net;
using ElinTogether.Patches;
using MessagePack;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class CharaPickThingDelta : ElinDeltaBase
{
    public enum PickType : byte
    {
        Pick,
        PickOrDrop,
        TrySmoothPick,
    }

    [Key(0)]
    public required RemoteCard Owner { get; init; }

    [Key(1)]
    public required RemoteCard Thing { get; init; }

    [Key(2)]
    public required Position? Pos { get; init; }

    [Key(3)]
    public required PickType Type { get; init; }

    public static bool CanApplyOnPC { get; set; }

    public override void Apply(ElinNetBase net)
    {
        // we do not apply to ourselves
        if (Owner.Find() is not Chara chara) {
            return;
        }

        if (!CanApplyOnPC && chara.IsPC) {
            return;
        }

        if (Thing.Find() is not Thing thing) {
            return;
        }

        // relay to clients
        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        switch (Type) {
            case PickType.Pick:
                chara.Stub_Pick(thing);
                break;
            case PickType.PickOrDrop:
                chara.Stub_PickOrDrop(Pos, thing);
                break;
            case PickType.TrySmoothPick:
                _map.Stub_TrySmoothPick(Pos, thing, chara);
                break;
        }
    }
}