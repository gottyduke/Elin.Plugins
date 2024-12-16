## Custom Whatever Loader
Allows the game to automatically load modders' custom resources from the mod directory, simplifying the process for modders to utilize various game functionalities without any additional steps, and with extended localization support.

Ideal for mods that introduce new items, characters, or audio elements, the Custom Whatever Loader eliminates the need for creating script DLLs to import Excel sheets.

## Features
- Source Sheets
- Custom Adventurer
- Dialog/Drama
- Book Text
- Sound/BGM

New features are added upon request.

## Example Mod Setup
Custom Whatever Loader requires your resources to be placed under the **LangMod** folder instead of **Lang**; otherwise, the game will duplicate the entire translation tree into your mod folder. Within the **LangMod** folder, you can add as many supported languages as you want by naming the subfolders with the language code, for example:
[img](https://i.postimg.cc/h4LqnrjS/image.png)

When Custom Whatever Loader imports the resources, it will import from the current language folder first, effectively addressing the translation issue with the current Elin xlsx implementation, which generally only has JP and EN entries.

## Custom Sources
Instead of calling `ModUtil.ImportExcel` on each xlsx worksheet manually, modders can now simply place the xlsx files within each language folder. Custom Whatever Loader will import all the localized sources according to the sheet name that matches a SourceData or SourceLang.

Note that it's the **sheet name**, not the file name! For example, this will import `SourceThing`, `SourceChara`, `LangGeneral` accordingly.
[img](https://i.postimg.cc/vZqGNjfC/Screenshot-1.png)

Supported source data are: 
```
Chara, CharaText, Thing, Element, Job, Obj, Quest, Race, Religion, Zone, Area, Backer, Block, Calc, Category, CellEffect, Check, Collectible, Faction, Floor, Food, GlobalTile, Hobby, HomeResource, KeyItem, Material, Person, Recipe, Research, SpawnList, Stat, Tactics, ThingV, ZoneAffix
```

Supported source lang are: 
```
General, Game, List, Word, Note
```

You may also split the sheets into multiple xlsx files for organizing. The xlsx file name doesn't matter.

If you want to browse what are IDs for in game things/charas/various sources, checkout [url=https://elin-modding-resources.github.io/Elin.Docs/]Elin Modding Wiki[/url]:
[img](https://i.postimg.cc/15wF6V2L/image.png)

## Custom Adventurer
To automatically add your custom character as adventurer to the game, make sure it has trait `Adventurer` or `AdventurerBacker`, and add `addAdvZone_*` to the tag column, replace the `*` with zone name or keep it for random zone. For example, a tag cell could look like `noPotrait,addAdvZone_Palmia`.
[img](https://i.postimg.cc/SN93258B/image.png)

To assign specific equipment to the adventurer, you can add an additional tag `addAdvEq_ItemID#Rarity`, where ItemID is replaced by the item's ID, and Rarity is one of the following: **Random, Crude, Normal, Superior, Legendary, Mythical, Artifact**. If `#Rarity` is omitted, the default rarity `#Random` will be used. For example, to set a legendary `BS_Flydragonsword` and a random `axe_machine` as the main weapons for the adventurer, use the following code:
```
addAdvZone_Palmia,addAdvEq_BS_Flydragonsword#Legendary,addAdvEq_axe_machine
```

To add starting items to the adventurer, you can use the tag `addAdvThing_ItemID#Count`. If `#Count` is omitted, a default of 1 item will be generated. For example:
```
addAdvZone_Palmia,addAdvThing_padoru_gift#10,addAdvThing_1174#5
```

You may add as many tags as you want. **Remember, tags are separated by commas (,) with no spaces in between.** If you need additional features, please let me know.

To see some CWL usage examples, checkout the following mods:

[The Eternal Student, Kubrika](https://steamcommunity.com/sharedfiles/filedetails/?id=3380350255)
[Miranda, Rookie Gunner](https://steamcommunity.com/sharedfiles/filedetails/?id=3383166653)
[Christmas Red Saber](https://steamcommunity.com/sharedfiles/filedetails/?id=3383191390])
[Una](https://steamcommunity.com/sharedfiles/filedetails/?id=3384670717)
[Kiria's Memory Quest DLC](https://steamcommunity.com/sharedfiles/filedetails/?id=3381789374)

## Custom Religion
Custom Whatever Loader can import the Religion source sheet and patch it into the game. However, your custom religion id must begin with `cwl_`, for example:
```
cwl_spaghettigod
```

By default the religion is joinable and not minor. To override it, append tags to the id:
- To set it as a minor god, append `#minor`
- To make it unjoinable, append `#cannot`

For example: **cwl_spaghettigod#minor#cannot** . However, do note that the actual id of your religion is still **cwl_spaghettigod**, the tags will be removed upon importing.

To setup a custom portrait for your religion, place a `.png` image into **YourMod/Texture** folder, with the same id as file name, e.g. `cwl_spaghettigod.png`.

Custom Whatever Loader can also merge your custom god_talk.xlsx into base game. You may reference the base game sheet at **Elin/Package/_Elona/Lang/EN/Data/god_talk.xlsx**.
[img](https://i.postimg.cc/P5V71tTq/image.png)

## Custom Sounds
Sound files should be in **wav** format, with the filename serving as the sound ID. A default metadata JSON is generated upon loading, allowing you to edit and apply sound file metadata upon the next game launch. **You can override existing in-game sounds using the same ID**. 

By setting "type": "BGM" in the metadata, the sound file will be instantiated as **BGMData** instead of **SoundData**. You can also customize the BGM parts in the metadata.

Subdirectories in the Sound folder will serve as ID prefixes. For example, AI_PlayMusic will use **Instrument/sound_id**, so you should place the sound file in the Instrument folder if you plan to replace instrument sounds.

## Having a Problem?
If you want to request new features, provide feedback, in need of assistance, feel free to leave comments or reach my at Elona Discord @freshcloth.

It's also helpful to check AppData/LocalLow/Lafrontier/Elin/Player.log if you encounter problem, Custom Whatever Loader logs **a lot** of stuff there.

[sauce](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader)