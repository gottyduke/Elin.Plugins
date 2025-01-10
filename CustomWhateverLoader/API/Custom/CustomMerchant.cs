using System.Collections.Generic;
using System.Linq;
using Cwl.API.Processors;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.LangMod;
using MethodTimer;

namespace Cwl.API.Custom;

public class CustomMerchant : TraitMerchant
{
    private static bool _transform;
    internal static readonly Dictionary<string, SerializableStockData> Managed = [];

    public static IReadOnlyCollection<SerializableStockData> All => Managed.Values;

    public override ShopType ShopType => ShopType.Specific;

    public static void AddStock(string id)
    {
        if (!_transform) {
            TraitTransformer.Add(TransformMerchant);
        }

        _transform = true;

        var file = PackageIterator.GetRelocatedFilesFromPackage($"Data/stock_{id}.json").FirstOrDefault();
        if (!ConfigCereal.ReadConfig(file?.FullName, out SerializableStockData? stock) || stock is null) {
            CwlMod.Warn("cwl_warn_stock_file".Loc(id));
            return;
        }

        Managed[id] = stock;
        // disabled due to changing hashes
        //ConfigCereal.WriteConfig(stock, file!.FullName);
    }

    [Time]
    public void Generate()
    {
        if (!Managed.TryGetValue(owner.id, out var stock)) {
            // somehow stock data is lost?
            return;
        }

        var inv = owner.things.Find("chest_merchant");
        if (inv is null) {
            return;
        }

        var noRestocks = player.noRestocks.TryGetValue(owner.id);
        noRestocks ??= [];

        foreach (var item in stock.Items) {
            try {
                if (sources.cards.map.TryGetValue(item.Id) is null) {
                    continue;
                }

                var thing = item.Type switch {
                    StockItemType.Item => ThingGen.Create(item.Id, ReverseId.Material(item.Material), ShopLv)
                        .SetNum(item.Num),
                    StockItemType.Recipe => ThingGen.CreateRecipe(item.Id),
                    StockItemType.Spell => ThingGen.CreateSpellbook(item.Id, item.Num),
                    _ => ThingGen.Create(item.Id),
                };

                if (!item.Restock && noRestocks.Contains(item.Id)) {
                    thing.Destroy();
                    continue;
                }

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

    private static void TransformMerchant(ref string traitName, Card traitOwner)
    {
        if (traitName == nameof(TraitMerchant) && Managed.Keys.Contains(traitOwner.id)) {
            traitName = nameof(CustomMerchant);
        }
    }
}