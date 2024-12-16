## How to Create Custom Adventurer

Credits to 105gun.

Assumes you already have your custom character defined in a Chara sheet, make sure it has trait **Adventurer** or **AdventurerBacker**.

To automatically add your custom character as adventurer to the game, add **addAdvZone_*** to the tag column, replace the * (asterisk) with zone name or keep it for random zone. For example, a tag cell could look like **noPotrait,addAdvZone_Palmia**.

![img](https://i.postimg.cc/SN93258B/image.png)

To assign specific equipment to the adventurer, you can add an additional tag **addAdvEq_ItemID#Rarity**, where ItemID is replaced by the item's ID, and Rarity is one of the following: **Random, Crude, Normal, Superior, Legendary, Mythical, Artifact**. If **#Rarity** is omitted, the default rarity **#Random** will be used. 

For example, to set a legendary **BS_Flydragonsword** and a random **axe_machine** as the main weapons for the adventurer:
**addAdvZone_Palmia,addAdvEq_BS_Flydragonsword#Legendary,addAdvEq_axe_machine**

To add starting items to the adventurer, you can use the tag **addAdvThing_ItemID#Count**. If **#Count** is omitted, a default of **1** item will be generated. 

For example, to add **padoru_gift** x10 and **scroll of ally** x5 to the adventurer:
**addAdvZone_Palmia,addAdvThing_padoru_gift#10,addAdvThing_1174#5**

You may add as many tags as you want. **Remember, tags are separated by , (comma) with no spaces in between**. If you need additional features, don't hesitate to ask!

## 自定义冒险者

感谢105gun提供的代码。

要在游戏中自动添加自定义角色为冒险者，确保 trait 设定为 **Adventurer** 或者 **AdventurerBacker**，并在 tag 中添加 **addAdvZone_*** ，将 * (星号) 替换为区域名称（英语）或保留 * 以使用随机区域。例如，tag 可以是 **noPotrait,addAdvZone_Palmia**。

![img](https://i.postimg.cc/SN93258B/image.png)

要为冒险者分配特定装备，可以添加额外的标签 **addAdvEq_ItemID#稀有度**，其中 ItemID 替换为物品的 ID，**稀有度** 是以下之一（英语）：**Random, Crude, Normal, Superior, Legendary, Mythical, Artifact** (**随机、粗糙、普通、优越、传奇、神话、神器**)。如果省略 **#稀有度**，将使用默认稀有度 **#Random**。

例如，要将传奇的 **BS_Flydragonsword** 和随机的 **axe_machine** 设置为冒险者的主武器：
**addAdvZone_Palmia,addAdvEq_BS_Flydragonsword#Legendary,addAdvEq_axe_machine**

要为冒险者添加起始物品，可以使用标签 **addAdvThing_ItemID#数量**。如果省略 **#数量**，将生成默认的 **1** 个物品。

例如，要为冒险者添加 **padoru_gift** x10 和 **援军卷轴** x5：
**addAdvZone_Palmia,addAdvThing_padoru_gift#10,addAdvThing_1174#5**

您可以添加任意数量的标签。**请记住，标签之间用 ,（英语逗号）分隔，中间不留空格**。如果需要其他功能，请告知。

## カスタム冒険者

105gunが提供したコードに感謝します。

ゲーム内でカスタムキャラクターを冒険者として自動的に追加するには、traitを**Adventurer**または**AdventurerBacker**に設定し、tagに**addAdvZone_*** を追加します。* （アスタリスク）を地域名（英語）に置き換えるか、* をそのままにしてランダムな地域を使用します。例えば、tagは**noPotrait,addAdvZone_Palmia**のようになります。

![img](https://i.postimg.cc/SN93258B/image.png)

冒険者に特定の装備を割り当てるには、追加のタグ**addAdvEq_ItemID#レアリティ**を追加します。ここで、ItemIDはアイテムのIDに置き換え、**レアリティ**は以下のいずれか（英語）：**Random, Crude, Normal, Superior, Legendary, Mythical, Artifact**（**ランダム、粗雑、普通、優れた、伝説、神話、アーティファクト**）です。**#レアリティ**を省略すると、デフォルトのレアリティ **#Random** が使用されます。

例えば、伝説の**BS_Flydragonsword**とランダムな**axe_machine**を冒険者の主武器に設定するには：
**addAdvZone_Palmia,addAdvEq_BS_Flydragonsword#Legendary,addAdvEq_axe_machine**

冒険者に初期アイテムを追加するには、タグ**addAdvThing_ItemID#数量**を使用します。**#数量**を省略すると、デフォルトの**1**個のアイテムが生成されます。

例えば、冒険者に**padoru_gift** x10と**援軍巻物** x5を追加するには：
**addAdvZone_Palmia,addAdvThing_padoru_gift#10,addAdvThing_1174#5**

任意の数のタグを追加できます。**タグの間は ,（英語のカンマ）で区切り、間にスペースを入れないでください**。他に必要な機能があればお知らせください。