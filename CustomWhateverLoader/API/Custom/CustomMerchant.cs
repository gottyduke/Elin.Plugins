using System.Collections.Generic;
using System.Linq;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.LangMod;
using ReflexCLI.Attributes;

namespace Cwl.API.Custom;

[ConsoleCommandClassCustomizer("cwl.stock")]
public class CustomMerchant : TraitMerchant
{
    internal static readonly List<SerializableStockData> Managed = [];

    private static ILookup<string, SerializableStockItem>? _lookup;

    public static ILookup<string, SerializableStockItem> All => _lookup ??= Managed
        .SelectMany(s => s.Items, (s, i) => new { s.Owner, Item = i })
        .ToLookup(s => s.Owner, s => s.Item);

    public override ShopType ShopType => ShopType.Specific;

    [ConsoleCommand("add")]
    public static void AddStock(string ownerId, string stockId = "")
    {
        stockId = stockId.IsEmpty(ownerId);

        var stock = GetStockData(stockId);
        if (stock is null) {
            CwlMod.WarnWithPopup<CustomMerchant>("cwl_warn_stock_file".Loc(stockId));
            return;
        }

        _lookup = null;

        var merge = Managed.Find(s => s.Owner == ownerId) is not null;
        var addon = stock.GetIntrospectCopy();
        addon.Owner = ownerId;

        Managed.Add(addon);

        var logEntry = merge ? "cwl_log_stock_merge" : "cwl_log_stock_add";
        CwlMod.Log<CustomMerchant>(logEntry.Loc(stockId, ownerId));
    }

    [ConsoleCommand("clear")]
    public static void ClearStock(string ownerId)
    {
        _lookup = null;
        Managed.RemoveAll(s => s.Owner == ownerId);
    }

    public static SerializableStockItem[] GetStockItems(string ownerId)
    {
        return All[ownerId].ToArray();
    }

    public static SerializableStockData? GetStockData(string stockId)
    {
        var stocks = PackageIterator.GetRelocatedJsonsFromPackage<SerializableStockData>($"Data/stock_{stockId}.json");
        return stocks.LastOrDefault().Item2;
    }

    public static void GenerateStock(Card owner, IEnumerable<SerializableStockItem> items, bool clear = false)
    {
        var inv = owner.things.Find("chest_merchant");
        if (inv is null) {
            inv = ThingGen.Create("chest_merchant");
            owner.AddThing(inv);
        }

        if (clear) {
            inv.RemoveThings();
        }

        var noRestocks = player.noRestocks.TryGetValue(owner.id);
        noRestocks ??= [];

        foreach (var item in items) {
            try {
                if (sources.cards.map.TryGetValue(item.Id) is null) {
                    continue;
                }

                if (!item.Restock && noRestocks.Contains(item.Id)) {
                    continue;
                }

                var thing = item.Create(owner.trait.ShopLv);
                inv.AddThing(thing);

                if (!item.Restock) {
                    noRestocks.Add(item.Id);
                    thing.SetInt(CINT.noRestock, 1);
                }

                player.noRestocks[owner.id] = noRestocks;
            } catch {
                // noexcept
            }
        }
    }

    // invoked by CWL
    internal void _OnBarter()
    {
        var stock = GetStockItems(owner.id);
        if (stock.Length > 0) {
            GenerateStock(owner, stock);
        }
    }

    internal static void TransformMerchant(ref string traitName, Card traitOwner)
    {
        if (traitName.StartsWith(nameof(TraitMerchant)) && GetStockItems(traitOwner.id).Any()) {
            traitName = nameof(CustomMerchant);
        }
    }
}