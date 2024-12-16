## How to Create Custom Religion

Custom Whatever Loader can import the Religion source sheet and patch it into the game. However, your custom religion ID must begin with **cwl_**, for example: **cwl_spaghettigod**.

By default the religion is **joinable** and **not minor**. You can append optional tags to the ID:
- To make it a minor god, append **#minor**
- To make it unjoinable, append **#cannot**

For example: **cwl_spaghettigod#minor#cannot** will make it a minor god religion and unjoinable. However, do note that the actual ID of your religion is still **cwl_spaghettigod**, the tags will be removed upon importing.

Custom Whatever Loader will merge your custom **god_talk.xlsx** into base game, this is necessary for the religion to function. You may reference the base game sheet at **Elin/Package/_Elona/Lang/EN/Data/god_talk.xlsx**.

![img](https://i.postimg.cc/P5V71tTq/image.png)

Your custom **god_talk.xlsx** should only contain the talks of your own religion ID, and be placed under **LangMod/*/Data** folder.

To create an optional custom portrait for your religion, put a **.png** image in the **Texture** folder using the same religion ID as the file name, such as **cwl_spaghettigod.png**.

## 自定义信仰

随便加载器可以导入您的自定义信仰表（表名: Religion）并将其添加到游戏中。然而，您的自定义信仰 ID 必须以 **cwl_** 开头，例如：**cwl_spaghettigod**。

默认情况下，该信仰是可加入的，并且不是弱神。您也可以将可选标签附加到 ID：
- 要将其设置为是弱神，请附加 **#minor**
- 要使其不可加入，请附加 **#cannot**

例如：**cwl_spaghettigod#minor#cannot** 会将其设置为弱神信仰并不可加入。但请注意，您的信仰的实际 ID 仍然是 **cwl_spaghettigod**，标签在导入时将被移除。

随便加载器会把您的自定义 **god_talk.xlsx** 合并到游戏中，此文件是必需的。您可以参考游戏的中文表格，路径为 **Elin/Packag/_Lang_Chinese/Lang/CN/Data/got_talk.xlsx**。

![img](https://i.postimg.cc/P5V71tTq/image.png)

您的自定义 **god_talk.xlsx** 应仅包含您自己信仰 ID 的对话，并放置在 **LangMod/*/Data** 文件夹下。

要为您的信仰创建一个可选的自定义肖像，请将 **.png** 图像放入 **Texture** 文件夹，使用与信仰 ID 相同的文件名，例如 **cwl_spaghettigod.png**。

## カスタム信仰

随便ロードは、あなたのカスタム信仰表（表名: Religion）をインポートし、ゲームに追加することができます。ただし、あなたのカスタム信仰IDは **cwl_** で始まる必要があります。例えば：**cwl_spaghettigod**。

デフォルトでは、この信仰は参加可能であり、弱神ではありません。また、IDにオプションのタグを追加することもできます：
- 弱神に設定するには、**#minor** を追加します。
- 参加不可にするには、**#cannot** を追加します。

例えば：**cwl_spaghettigod#minor#cannot** は、弱神の信仰として設定され、参加できなくなります。しかし、あなたの信仰の実際のIDは依然として **cwl_spaghettigod** であり、インポート時にタグは削除されます。

随便ロードは、あなたのカスタム **god_talk.xlsx** をゲームに統合します。このファイルは必須です。ゲームの表を参照できます。パスは **Elin/Package/_Elona/Lang/JP/Data/god_talk.xlsx** です。

![img](https://i.postimg.cc/P5V71tTq/image.png)

あなたのカスタム **god_talk.xlsx** には、自分の信仰IDの対話のみを含めるべきです，**LangMod/*/Data** フォルダーに置いてください。

あなたの信仰のためにオプションのカスタム肖像を作成するには、**.png** 画像を **Texture** フォルダに入れ、信仰IDと同じファイル名を使用します。例えば **cwl_spaghettigod.png** です。