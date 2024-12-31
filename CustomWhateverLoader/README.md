## Custom Whatever Loader

![Version](https://img.shields.io/badge/Version-1.16.0-R.svg)

[中文](./README.CN.md)  
[日本語](./README.JP.md)  

Allows the game to automatically load modders' custom resources from the mod directory, simplifying the process for modders to utilize various game functionalities without any additional steps, and with extended localization support.

Ideal for mods that introduce new items, characters, or audio elements, the Custom Whatever Loader eliminates the need for creating script DLLs to import Excel sheets.

## Using CWL

- Source Sheets (Character, Items, Races, Talks, etc)
- Custom Adventurer
- Custom Merchant
- Custom Element (Ability, Spell)
- Custom Religion
- Custom Material (New color)
- Dialog/Drama
- Book Text
- Localization Support 
- Sound/BGM
- Lots of Fixes & Optimizations
    - Unified import process to reduce load time
    - Auto detect incompatible sheet
    - Rethrow excel parsing exceptions with more details
    - Safely load game with invalid modded elements/cards/quests
- Comprehensive API

CWL is made with community effort and feedback, new features are added upon request.

## Example Mod Setup

Custom Whatever Loader requires your resources to be placed under the **LangMod** folder instead of **Lang**; otherwise, the game will duplicate the entire translation tree into your mod folder. Within the **LangMod** folder, you can add as many supported languages as you want by naming the subfolders with the language code, for example:

![img](https://i.postimg.cc/tJypn1Ys/image.png)

When Custom Whatever Loader imports the resources, it will import from the current language folder first, effectively addressing the translation issue with the current Elin xlsx implementation, which generally only has JP and EN entries.

## Custom Sources

Instead of calling **ModUtil.ImportExcel** on each xlsx worksheet manually, modders can now simply place the xlsx files within each language folder. Custom Whatever Loader will import all the localized sources according to the sheet name that matches a SourceData or SourceLang.

Note that it's the **sheet name**, not the file name! For example, this will import **SourceThing**, **SourceChara**, **LangGeneral** accordingly.

![img](https://i.postimg.cc/vZqGNjfC/Screenshot-1.png)

Supported SourceData are: 
```
Chara, CharaText, Thing, Race, Element, Job, Obj, Material, Quest, Religion, Zone, Area, Block, Category, CellEffect, Collectible, Faction, Floor, Food, GlobalTile, Hobby, HomeResource, KeyItem, Person, Recipe, Research, SpawnList, Stat, Tactics, ThingV
```

Supported SourceLang are: 
```
General, Game, List, Word, Note
```

You may also split the sheets into multiple xlsx files for organizing. The xlsx file name doesn't matter.

If you want to browse the IDs for in game things/charas/various sources, checkout [Elin Modding Wiki](https://elin-modding-resources.github.io/Elin.Docs):

![img](https://i.postimg.cc/15wF6V2L/image.png)

## Special Imports

[How to Import Custom Adventurer](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/CustomAdventurer.md)  
[How to Setup Custom Merchant](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/CustomMerchant.md)  
[How to Import Custom Religion](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/CustomReligion.md)  
[How to Import Custom Ability/Spell](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/CustomElement.md)  
[How to Import Custom Material/Color](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/CustomMaterial.md)  
[How to Import Custom Sound](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/CustomSound.md)  

## Usage Examples

To see some CWL usage examples, checkout the following mods (and more):

[Wakaba Mutsumi](https://steamcommunity.com/sharedfiles/filedetails/?id=3380127472)  
[The Eternal Student: Kubrika](https://steamcommunity.com/sharedfiles/filedetails/?id=3380350255)  
[Miranda, Rookie Gunner](https://steamcommunity.com/sharedfiles/filedetails/?id=3383166653)  
[Christmas Red Saber](https://steamcommunity.com/sharedfiles/filedetails/?id=3383191390)  
[Fairy Dust: Una](https://steamcommunity.com/sharedfiles/filedetails/?id=3384670717)  
[Kiria's Memory Quest DLC](https://steamcommunity.com/sharedfiles/filedetails/?id=3381789374)  
[Drill Legend: Reincarnation Eiln KasaneTeto](https://steamcommunity.com/sharedfiles/filedetails/?id=3385442190)  
[「オブザーバー」種族追加MOD](https://steamcommunity.com/sharedfiles/filedetails/?id=3385578698)  
[Custom Instrument Track](https://steamcommunity.com/sharedfiles/filedetails/?id=3374708172)

## API

By referencing CustomWhateverLoader.dll, you gain access to the entire **CWL.API** and **CWL.Helper** namespaces, please check out the GitHub source code down below for the specifics.

Please do not ship the CustomWhateverLoader.dll with your mod if using API via referencing dll.

## Code Localization

You may export the string entries to a General sheet and let Custom Whatever Loader import it to LangGeneral, then you can use **"my_lang_str".lang()** to localize in code at runtime.

![img](https://i.postimg.cc/76HS3t8M/image.png)

## Configuration

[Configure CWL Functions](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/Config.md)  

## Change Logs

**1.13** Added support for custom material, lots of safe loading optimizations.  
**1.12** Added support for custom abilities and spells.  
**1.11** Fixed a cryptic bug where CWL attempts to do its funny things for other mods.  
**1.10** Added support for custom religion imports and custom religion/domain/faction portraits.  
**1.9** Added auto detection for incompatible source sheets and sheet header realignment. Configurable.  
**1.8** Added custom adventurer tagging for equipment/things.  
**1.7** Set EN as 1st fallback language.  
**1.6** Added support for custom adventurer related imports and dialog.xlsx merging.  
**1.5** API refactor.  
**1.4** Added source sheet imports with localization support.  
**1.3** Fixed BGMData.Part duplicating first entry.  
**1.2** Added support for custom sounds.  
**1.1** Added support for book texts.  
**1.0** Added support for dialogs/drama sheets.  

## Having a Problem?

If you want to request new features, provide feedback, in need of assistance, feel free to leave comments or reach my at Elona Discord @freshcloth.

Should any bug appear, don't forget to check **YourUserName/AppData/LocalLow/Lafrontier/Elin/Player.log**, Custom Whatever Loader logs **a lot** of stuff there.

[sauce](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader)
