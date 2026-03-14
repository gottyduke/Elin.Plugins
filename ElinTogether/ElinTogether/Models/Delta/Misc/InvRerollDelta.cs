using ElinTogether.Net;
using MessagePack;
using UnityEngine;

namespace ElinTogether.Models.ElinDelta;

[MessagePackObject]
public class InvRerollDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Owner { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (Owner.Find() is not { } owner) {
            return;
        }

        var cost = owner.trait.CostRerollShop;
        var inv = LayerInventory.listInv.Find(l => l.invs[0].owner.owner == owner)?.invs[0];
        if (net.IsClient) {
            EMono._zone.influence -= cost;
            owner.c_dateStockExpire = world.date.GetRaw(24 * owner.trait.RestockDay);
            if (inv is null) {
                return;
            }

            inv.RefreshGrid();
            inv.Sort();
            SE.Dice();
            SE.Play("shop_open");

            return;
        }

        if (EMono._zone.influence < cost) {
            return;
        }

        EMono._zone.influence -= cost;
        owner.c_dateStockExpire = 0;
        owner.trait.OnBarter(reroll: true);

        net.Delta.AddRemote(this);

        if (inv is null) {
            return;
        }

        inv.RefreshGrid();
        inv.Sort();
        SE.Dice();
        SE.Play("shop_open");
    }
}