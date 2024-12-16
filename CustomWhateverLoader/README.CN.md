## 随便加载器

允许游戏自动从模组目录加载模组制作者的自定义资源，简化了模组制作者利用各种游戏功能的过程，无需额外步骤，并扩展了本地化支持。

非常适合引入新物品、角色、元素、或音频的模组，CWL免去使用DLL只为导入表格的麻烦。

## 支持
- 源表（角色、物品、种族、对话等）
- 自定义冒险者
- 自定义信仰
- 对话/剧情
- 书籍文本
- 声音/BGM

根据需求添加新功能。

## 示例模组设置

CWL要求Mod放置在**LangMod**文件夹下，而不是**Lang**；否则，游戏将把整个翻译树复制到您的模组文件夹中。在**LangMod**文件夹中，您可以通过使用语言代码命名子文件夹来添加任意数量的支持语言，例如：

![img](https://i.postimg.cc/h4LqnrjS/image.png)

当CWL导入资源时，它将优先从当前语言文件夹导入，有效解决了当前Elin xlsx的翻译问题，因为大部分工作表通常只包含JP和EN条目。

## 自定义源表

您可以将 xlsx 文件简单地放置在每个语言文件夹中，而不必手动为每个 xlsx 工作表上调用 **ModUtil.ImportExcel**。CWL将根据与 SourceData 或 SourceLang 匹配的表名导入所有本地化的源。

请注意是 **表名**，而不是文件名！例如，这将相应地导入 **SourceThing**, **SourceChara**, **LangGeneral**。
![img](https://i.postimg.cc/vZqGNjfC/Screenshot-1.png)

支持的 SourceData：
```
Chara, CharaText, Thing, Race, Element, Job, Obj, Quest, Religion, Zone, Area, Backer, Block, Calc, Category, CellEffect, Check, Collectible, Faction, Floor, Food, GlobalTile, Hobby, HomeResource, KeyItem, Material, Person, Recipe, Research, SpawnList, Stat, Tactics, ThingV, ZoneAffix
```

支持的 SourceLang：
```
General, Game, List, Word, Note
```

您也可以将工作表拆分成多个 xlsx 文件以方便管理。xlsx 文件名无关紧要。

如果您想浏览游戏中物品/角色/各种源的 ID，请查看 [Elin Modding Wiki](https://elin-modding-resources.github.io/Elin.Docs)：

![img](https://i.postimg.cc/15wF6V2L/image.png)

## 自定义冒险者

想制作您的自定义冒险者吗？请查看[如果制作自定义冒险者](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/CustomAdventurer.md)

## 自定义信仰

想制作您的自定义信仰吗？请查看[如何制作自定义信仰](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/CustomReligion.md)

## 自定义音频

音频文件应为 **wav** 格式，文件名作为音频ID。加载时会生成默认的同名元数据JSON，允许您编辑并在在下次游戏启动时应用音频文件元数据。**您可以使用相同的ID覆盖现有的游戏内音频**。

通过在元数据中设置"type": "BGM"，音频将作为**BGMData**而不是**SoundData**实例化。您还可以在元数据中自定义BGM的小节部分。

**Sound**文件夹中的子目录将作为音频ID前缀。例如，AI_PlayMusic将使用Instrument/sound_id，因此如果您打算替换乐器音乐，应该将同名音频文件放在Instrument文件夹中。

## 使用示例

要查看一些CWL使用示例，请查看以下模组（以及更多）：

[若葉睦](https://steamcommunity.com/sharedfiles/filedetails/?id=3380127472)

[永远JK: 库布莉卡](https://steamcommunity.com/sharedfiles/filedetails/?id=3380350255)

[米兰达, 菜鸟炮手](https://steamcommunity.com/sharedfiles/filedetails/?id=3383166653)

[圣诞红Saber](https://steamcommunity.com/sharedfiles/filedetails/?id=3383191390)

[妖精之尘: 乌娜](https://steamcommunity.com/sharedfiles/filedetails/?id=3384670717)

[机利亚的记忆, 任务DLC](https://steamcommunity.com/sharedfiles/filedetails/?id=3381789374)

[鑽頭無双: 重音テト](https://steamcommunity.com/sharedfiles/filedetails/?id=3385442190)

[「オブザーバー」種族追加](https://steamcommunity.com/sharedfiles/filedetails/?id=3385578698)

## API

通过引用CustomWhateverLoader.dll，您可以访问整个**CWL.API**和**CWL.Helper**命名空间，请查看GitHub相关源代码。

如果通过引用dll使用API，请勿将CustomWhateverLoader.dll与您的模组一同发布。

## 代码本地化

您可以将文本条目导出到一个General表，并让Custom Whatever Loader将其导入到LangGeneral中，然后您可以在运行时使用 **"my_lang_str".lang()** 进行代码本地化。

![img](https://i.postimg.cc/wMSyvD8D/image.png)

## 更新日志

**1.11** 修复了CWL试图对其他模组做一些奇怪的事情的错误。  
**1.10** 添加了对自定义信仰导入和自定义信仰/领域/派系肖像的支持。  
**1.9** 添加了对不兼容源表的自动检测和表头重新对齐。可开关。  
**1.8** 为装备/物品添加了自定义冒险者标签。  
**1.7** 将英语设为第一备用语言。  
**1.6** 添加了对自定义冒险者相关导入和dialog.xlsx合并的支持。  
**1.5** API重构。  
**1.4** 添加了带本地化支持的源表导入。  
**1.3** 修复了BGMData.Part重复第一个条目的问题。  
**1.2** 添加了对自定义音频的支持。  
**1.1** 添加了对书籍文本的支持。  
**1.0** 添加了对对话/剧情的支持。

## 遇到问题了吗？

如果您需要新功能、提供反馈或制作帮助，请随时留言或通过 Elona Discord 联系我 @freshcloth。

如果出现任何错误，请不要忘记检查 **%LOCALAPPDATA%Low/Lafrontier/Elin/Player.log**，CWL 会在那里记录 **很多** 信息。

[sauce](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader)
