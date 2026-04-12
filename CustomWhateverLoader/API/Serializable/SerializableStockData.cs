using System.Collections.Generic;
using Cwl.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cwl.API;

#pragma warning disable CS0649
#pragma warning disable CS0414
// ReSharper disable All 
public sealed record SerializableStockData : SerializableStockDataV1;

public sealed record SerializableStockItem : SerializableStockItemV3;

public record SerializableStockDataV1
{
    public List<SerializableStockItem> Items = [];
    public string Owner = "";
}

public enum StockItemType
{
    Item,
    Block,
    Cassette,
    Currency,
    Category,
    Filter,
    Tag,
    Letter,
    Map,
    Obj,
    Perfume,
    Plan,
    Potion,
    Recipe,
    RedBook,
    Rod,
    Rune,
    RuneFree,
    Scroll,
    Skill,
    Spell,
    Usuihon,
}

public record SerializableStockItemV3 : SerializableStockItemV2
{
    [JsonProperty]
    public bool Identified = true;

    [JsonProperty]
    [JsonConverter(typeof(StringEnumConverter))]
    public BlessedState Blessed = BlessedState.Normal;

    [JsonProperty]
    [JsonConverter(typeof(StringEnumConverter))]
    public IDTLevel? IdentifyLevel = null;

    public Thing Create(int lv = -1)
    {
        CardBlueprint.SetRarity(Rarity);
        int.TryParse(Id, out var intId);
        if (EMono.sources.elements.fuzzyAlias.TryGetValue(Id, out var alias)) {
            intId = EMono.sources.elements.alias[alias].id;
        }

        Thing thing = ThingGen.Create(Id);
        switch (Type) {
            case StockItemType.Item:
                thing = ThingGen.Create(Id, ReverseId.Material(Material), lv).SetNum(Num);
                break;
            case StockItemType.Block:
                thing = ThingGen.CreateBlock(ReverseId.Block(Id), ReverseId.Material(Material)).SetNum(Num);
                break;
            case StockItemType.Cassette:
                if (!EClass.core.refs.dictBGM.ContainsKey(intId)) {
                    intId = EClass.core.refs.dictBGM.RandomItem().id;
                }
                thing = ThingGen.CreateCassette(intId);
                break;
            case StockItemType.Currency:
                thing = ThingGen.CreateCurrency(Num, Id);
                break;
            case StockItemType.Category:
                thing = ThingGen.CreateFromCategory(Id, lv);
                break;
            case StockItemType.Filter:
                thing = ThingGen.CreateFromFilter(Id, lv);
                break;
            case StockItemType.Tag:
                thing = ThingGen.CreateFromTag(Id, lv);
                break;
            case StockItemType.Letter:
                thing = ThingGen.CreateLetter(Id);
                break;
            case StockItemType.Map:
                thing = ThingGen.CreateMap(Id, lv);
                break;
            case StockItemType.Obj:
                thing = ThingGen.CreateObj(ReverseId.Obj(Id), lv);
                break;
            case StockItemType.Perfume:
                thing = ThingGen.CreatePerfume(intId, lv);
                break;
            case StockItemType.Plan:
                thing = ThingGen.CreatePlan(intId);
                break;
            case StockItemType.Potion:
                thing = ThingGen.CreatePotion(intId, Num);
                break;
            case StockItemType.Recipe:
                thing = ThingGen.CreateRecipe(Id);
                break;
            case StockItemType.RedBook:
                thing = ThingGen.CreateRedBook(Id, Num);
                break;
            case StockItemType.Rod:
                thing = ThingGen.CreateRod(intId, Num);
                break;
            case StockItemType.Rune:
                thing = ThingGen.CreateRune(intId, Num);
                break;
            case StockItemType.RuneFree:
                thing = ThingGen.CreateRune(intId, Num, true);
                break;
            case StockItemType.Scroll:
                thing = ThingGen.CreateScroll(intId, Num);
                break;
            case StockItemType.Skill:
                thing = ThingGen.CreateSkillbook(intId, Num);
                break;
            case StockItemType.Spell:
                thing = ThingGen.CreateSpellbook(intId, Num);
                break;
            case StockItemType.Usuihon:
                thing = ThingGen.Create("1084");
                thing.c_idRefName = EClass.game.religions.dictAll.GetValueOrDefault(Id)?.id;
                break;
        }

        thing.c_IDTState = (int)(Identified ? IDTLevel.Identified : IDTLevel.RequireSuperiorIdentify);
        if (IdentifyLevel.HasValue) {
            thing.c_IDTState = (int)IdentifyLevel.Value;
        }

        thing.SetBlessedState(Blessed);

        return thing;
    }
}

public record SerializableStockItemV2 : SerializableStockItemV1
{
    [JsonProperty]
    [JsonConverter(typeof(StringEnumConverter))]
    public Rarity Rarity = Rarity.Normal;
}

[JsonObject(MemberSerialization.OptIn)]
public record SerializableStockItemV1
{
    [JsonProperty]
    public string Id = "";
    [JsonProperty]
    public string Material = "";
    [JsonProperty]
    public int Num = 1;
    [JsonProperty]
    public bool Restock = true;
    [JsonProperty]
    [JsonConverter(typeof(StringEnumConverter))]
    public StockItemType Type = StockItemType.Item;
}
// ReSharper restore All 