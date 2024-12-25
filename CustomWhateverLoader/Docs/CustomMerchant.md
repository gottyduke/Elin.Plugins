## How to Setup Custom Merchant

Sometimes you want your character to be a merchant and sell a predefined stock of items, be it your custom new items, recipes, or spellbooks. 

CWL allows you to use `addStock` tag on a Chara with `Merchant` trait to set their merchant stock, and optionally spawn them in a zone with `addZone_ZoneName`.

![img](https://i.postimg.cc/59gzM54K/image.png)

The stock file is a simple json file placed in your `LangMod/**/Data/` folder, with name `stock_merchantID.json`, for example, you have a custom Chara with id `example_merchant`, you should have a file `stock_example_merchant.json` in `LangMod/EN/Data/`, `LangMod/CN/Data/`, ...and other language sub folders.

Within the stock file, it's simply as follows:
```json
{
  "Owner": "example_merchant",
  "Items": [
    {
      "Id": "example_item",
      "Material": "",
      "Num": 1,
      "Restock": true,
      "Type": "Item"
    },
    {
      "Id": "example_item_limited",
      "Material": "granite",
      "Num": 1,
      "Restock": false,
      "Type": "Item"
    },
    {
      "Id": "example_item_craftable",
      "Material": "",
      "Num": 1,
      "Restock": false,
      "Type": "Recipe"
    }
  ]
}
```

The `Owner` value is the same as the merchant Chara id, and `Items` is an array of items in the stock. 

Item `Id` is the id of the Thing. `Material` is the material you want it to be sold as, leave it blank for default material defined in Thing row. `Num` is the count of the items in the stack. `Restock` defines whether it's a limited time item that can only be bought once when `false`. `Type` can be `Item`, `Recipe`, or `Spell`.

If you are not using a code editor, please use [JSONLint](https://jsonlint.com/) to validate your json.

## 自定义商人

有时您希望您的角色成为商人，并出售预定义的物品库存，无论是自定义的新物品、配方还是法术书。

CWL允许您在具有`Merchant`特征的角色上使用`addStock`标签来设置其自定义库存，并可选择使用`addZone_ZoneName`在某个区域生成他们。

![img](https://i.postimg.cc/59gzM54K/image.png)

库存文件是一个简单的json文件，放置在您的`LangMod/**/Data/`文件夹中，名称为`stock_merchantID.json`，例如，如果您有一个id为`example_merchant`的自定义角色，您应该在`LangMod/CN/Data/`、`LangMod/EN/Data/`等语言子文件夹中有一个文件`stock_example_merchant.json`。

在库存文件中，它的格式如下：
```json
{
  "Owner": "example_merchant",
  "Items": [
    {
      "Id": "example_item",
      "Material": "",
      "Num": 1,
      "Restock": true,
      "Type": "Item"
    },
    {
      "Id": "example_item_limited",
      "Material": "granite",
      "Num": 1,
      "Restock": false,
      "Type": "Item"
    },
    {
      "Id": "example_item_craftable",
      "Material": "",
      "Num": 1,
      "Restock": false,
      "Type": "Recipe"
    }
  ]
}
```

`Owner`值与商人角色id相同，`Items`是库存中物品的数组。

`Id`是物品的id。`Material`是您希望其出售时所用的材料，如果留空则使用预定义的材料。`Num`是堆叠中物品的数量。`Restock`定义它是否能够刷新库存，`false`只能购买一次。`Type`可以是`Item`、`Recipe`或`Spell` (物品、配方、法术书)。

如果您不使用代码编辑器，请使用[JSONLint](https://jsonlint.com/)验证您的json。

## カスタム商人

時には、キャラクターを商人にして、カスタムの新しいアイテム、レシピ、または魔法書など、予め定義されたアイテムの在庫を販売させたい場合があります。

CWLでは、`Merchant`特性を持つキャラクターに`addStock`タグを使用してカスタム在庫を設定することができ、さらに`addZone_ZoneName`を使用して特定のエリアに生成することも選択できます。

![img](https://i.postimg.cc/59gzM54K/image.png)

在庫ファイルはシンプルなjsonファイルで、あなたの`LangMod/**/Data/`フォルダーに配置し、`stock_merchantID.json`という名前にします。例えば、`example_merchant`というidを持つカスタムキャラクターがいる場合、`LangMod/EN/Data/`や`LangMod/CN/Data/`などの言語サブフォルダー内に`stock_example_merchant.json`というファイルが必要です。

在庫ファイルのフォーマットは次の通りです：
```json
{
  "Owner": "example_merchant",
  "Items": [
    {
      "Id": "example_item",
      "Material": "",
      "Num": 1,
      "Restock": true,
      "Type": "Item"
    },
    {
      "Id": "example_item_limited",
      "Material": "granite",
      "Num": 1,
      "Restock": false,
      "Type": "Item"
    },
    {
      "Id": "example_item_craftable",
      "Material": "",
      "Num": 1,
      "Restock": false,
      "Type": "Recipe"
    }
  ]
}
```

`Owner`値は商人キャラクターのidと同じで、`Items`は在庫内のアイテムの配列です。

`Id`はアイテムのidです。`Material`は販売時に使用される材料で、空白の場合は予め定義された材料が使われます。`Num`はスタック内のアイテムの数量です。`Restock`は在庫が補充可能かどうかを定義し、`false`の場合は一度しか購入できません。`Type`は`Item`、`Recipe`、または`Spell`（アイテム、レシピ、魔法書）です。

コードエディタを使用していない場合は、[JSONLint](https://jsonlint.com/)を使用してjsonを検証してください。