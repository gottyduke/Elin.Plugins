using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class OnBarterDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard ShopOwner { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (ShopOwner.Find() is not { } shopOwner) {
            return;
        }

        if (net.IsHost) {
            shopOwner.trait.OnBarter();
            return;
        }

        shopOwner.c_dateStockExpire = world.date.GetRaw(24 * shopOwner.trait.RestockDay);

        var inv = LayerInventory.listInv.Find(l => l.invs[0].owner.owner == shopOwner)?.invs[0];
        if (inv is null) {
            return;
        }

        var invOwnerShop = inv.owner;
        if (!CardCache.Contains(invOwnerShop.Container)) {
            // remove the temporary merchant chest
            shopOwner.things.Remove(invOwnerShop.Container.Thing);
            // replace it with the real merchant chest
            invOwnerShop.Container = shopOwner.things.Find("chest_merchant");
        }

        inv.RefreshGrid();
        inv.Sort();
    }
}