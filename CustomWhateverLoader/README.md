# Custom Whatever Loader

Patches the game to automatically load modders' custom resources from the mod directory, simplifying the process for modders to utilize various game functionalities without any additional steps, and with extended localization support.

Ideal for mods that introduce new items, characters, or audio elements, the Custom Whatever Loader eliminates the need for creating script DLLs to import Excel sheets.

## Features
+ SourceSheets
+ Custom Adventurer
+ Dialog/Drama
+ Book Text
+ Sound/BGM

New features are added upon request.

## Example Mod Setup

Custom Whatever Loader requires your resources to be placed under the [b]LangMod[/b] folder instead of [b]Lang[/b]; otherwise, the game will duplicate the entire translation tree into your mod folder. Within the [b]LangMod[/b] folder, you can add as many supported languages as you want by naming the subfolders with the language code, for example:

![](https://i.postimg.cc/1t4Dgkgw/Screenshot-2.png)

When Custom Whatever Loader imports the resources, it will import from the current language folder first, effectively addressing the translation issue with the current Elin xlsx implementation, which generally only has JP and EN entries.

## Usage Example
```
Msg.Say("my_langGame_str");
string localized = "my_langGeneral_str".lang();

ShowDialog("my_quest_sheet");
Book.Show("my_book_scroll");

PlaySound("my_sound_id");

Chara myNpc = CharaGen.Create("my_npc_id");
Thing myItem = ThingGen.Create("my_item_id");

spawn my_new_npc 1
spawn my_item 1 wood
```

## SourceSheet

Instead of calling `ModUtil.ImportExcel` on each xlsx worksheet manually, modders can now simply place the xlsx files within each language folder. Custom Whatever Loader will import all the localized sources according to the sheet name that matches a SourceData.

Note that it's the [b]sheet name[/b], not the file name! For example, this will import `SourceThing`, `SourceChara`, `LangGeneral` accordingly.

![](https://i.postimg.cc/vZqGNjfC/Screenshot-1.png)

You may also split the sheets into multiple xlsx files for organizing.

## Custom Adventurer

To automatically add your custom character as adventurer to the game, add `addAdvZone_*` to the tag column,replace the `*` with zone name or keep it for random zone. For example, a tag cell could look like `noPotrait,addAdvZone_Palmia`.

## Sound Files

Sound files should be in [b]wav[/b] format, with the filename serving as the sound ID. A default metadata JSON is generated upon loading, allowing you to edit and apply sound file metadata upon the next game launch. You can override existing in-game sounds using the same ID. Subdirectories in the Sound folder serve as ID prefixes, such as placing files in the Instrument folder for Instrument sound replacements.

By setting "type": "BGM" in the metadata, the sound file will be instantiated as [b]BGMData[/b] instead of [b]SoundData[/b]. You can also customize the BGM parts in the metadata.

Subdirectories in the Sound folder will serve as ID prefixes. For example, AI_PlayMusic will use [b]Instrument/sound_id[/b], so you should place the sound file in the Instrument folder if you plan to replace instrument sounds.

## Having a Problem?

If you want to request new features, provide feedback, in need of assistance, feel free to leave comments or reach my at Elona Discord @freshcloth.
[sauce](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomDialogLoader)
