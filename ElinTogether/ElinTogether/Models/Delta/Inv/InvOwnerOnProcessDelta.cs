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
            || Dest.Find() is not { } dest) {
            return;
        }

        if (thing.parent is not null && thing.parent != parent) {
            return;
        }

        if (net.IsHost) {
            net.Delta.AddRemote(this);
        }

        InvOwnerDraglet? destInv = null;
        switch (dest.trait) {
            case TraitChara:
                destInv = new InvOwnerGive(dest) {
                    chara = dest as Chara,
                };
                break;
            case TraitAltarChaos altarChaos:
                destInv = new InvOwnerChaosOffering(dest) {
                    altar = altarChaos,
                };
                break;
            case TraitAltar altar:
                destInv = new InvOwnerOffering(dest) {
                    altar = altar,
                };
                break;
            case TraitBank:
                destInv = new InvOwnerDeliver(dest) {
                    mode = InvOwnerDeliver.Mode.Bank,
                };
                break;
            case TraitFarmChest:
                destInv = new InvOwnerDeliver(dest) {
                    mode = InvOwnerDeliver.Mode.Crop,
                };
                break;
            case TraitTaxChest:
                destInv = new InvOwnerDeliver(dest) {
                    mode = InvOwnerDeliver.Mode.Tax,
                };
                break;
            case TraitCrafter crafter:
                destInv = new InvOwnerCraft(dest) {
                    crafter = crafter,
                };
                break;
            case TraitRecycle recycle:
                destInv = new InvOwnerRecycle(dest) {
                    recycle = recycle,
                };
                break;
            case TraitGacha gacha:
                destInv = new InvOwnerGacha(dest) {
                    gacha = gacha,
                };
                break;
            default:
                break;
        }

        destInv?._OnProcess(thing);
    }
}