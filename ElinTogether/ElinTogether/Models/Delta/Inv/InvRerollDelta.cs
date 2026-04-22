using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class InvRerollDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard ShopOwner { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (ShopOwner.Find() is not { } shopOwner) {
            return;
        }

        var cost = shopOwner.trait.CostRerollShop;
        var inv = LayerInventory.listInv.Find(l => l.invs[0].owner.owner == shopOwner)?.invs[0];
        if (net.IsClient) {
            EMono._zone.influence -= cost;
            shopOwner.c_dateStockExpire = world.date.GetRaw(24 * shopOwner.trait.RestockDay);
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
        shopOwner.c_dateStockExpire = 0;
        shopOwner.trait.OnBarter(true);

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