﻿using System.Collections.Generic;
using System.Linq;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.LangMod;

namespace Cwl.API.Custom;

public class CustomMerchant : TraitMerchant
{
    internal static readonly Dictionary<string, SerializableStockData> Managed = [];

    public static IReadOnlyCollection<SerializableStockData> All => Managed.Values;

    public override ShopType ShopType => ShopType.Specific;

    public static void AddStock(string charaId, string stockId)
    {
        stockId = stockId.IsEmpty() ? $"stock_{charaId}" : $"stock{stockId}";

        var file = PackageIterator.GetRelocatedFilesFromPackage($"Data/{stockId}.json").FirstOrDefault();
        if (!ConfigCereal.ReadConfig(file?.FullName, out SerializableStockData? stock) || stock is null) {
            CwlMod.WarnWithPopup<CustomMerchant>("cwl_warn_stock_file".Loc(stockId));
            return;
        }

        var merge = !Managed.TryAdd(charaId, stock);
        if (merge) {
            Managed[charaId].Items.AddRange(stock.Items);
        }

        CwlMod.Log<CustomMerchant>($"{(merge ? "merge" : "added")} {stockId} >> {charaId}");
    }

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

                if (!item.Restock && noRestocks.Contains(item.Id)) {
                    continue;
                }

                var thing = item.Create(ShopLv);
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

    internal static void TransformMerchant(ref string traitName, Card traitOwner)
    {
        if (traitName == nameof(TraitMerchant) && Managed.Keys.Contains(traitOwner.id)) {
            traitName = nameof(CustomMerchant);
        }
    }
}