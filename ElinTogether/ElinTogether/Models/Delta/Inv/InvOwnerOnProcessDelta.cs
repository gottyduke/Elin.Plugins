using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class InvOwnerOnProcessDelta : ElinDelta
{
    [Key(1)]
    public required RemoteCard Parent { get; init; }

    [Key(2)]
    public required RemoteCard Thing { get; init; }

    [Key(3)]
    public required RemoteCard Dest { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (Parent.Find() is not { } parent
            || Thing.Find() is not Thing { isDestroyed: false } thing
            || Dest.Find() is not Thing dest) {
            return;
        }

        if (thing.parent is not null && thing.parent != parent) {
            return;
        }

        // if (LayerInventory.CreateContainer(dest) is not { } layerInv) {
        //     return;
        // }

        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        // var destInv = layerInv.invs[0].owner;
        // destInv.OnProcess(thing);
    }
}