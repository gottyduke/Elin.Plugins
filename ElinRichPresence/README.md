# Requires Fixed Package Loader as of Elin 0.23.26

## Elin Rich Presence (ERPC)
Enhance your Discord experience by adding rich presence for Elin, allowing the entire server to stalk admire your succubus pianist...

Supported languages: 简体中文, English, Japanese

![base building](https://i.postimg.cc/tTY6vdXY/base-build.png)
![travel](https://i.postimg.cc/mk9HBb14/travel.png)
![nefia](https://i.postimg.cc/ZR2dG9Rj/nefia.png)
![explore](https://i.postimg.cc/TPXG4Tfm/town.png)

## Features

- Display your class icon, class information, and race information (on hover)
- Display your current map, danger level, and date (on hover)
- Show cover images for different zones (more zones are being added; creating art assets takes time)
- Customize presence phrases

## TODO

Add more images for different zones in the following order:
- More towns
  - Derphy ✔
  - Aquli Teola ✔
- Nefias
- Notable locations
- Different biomes
- Custom class icons

## Configuration

Configurations can be changed in `Elin\BepInEx\config\dk.elinplugins.discordrpc.cfg`.

- `LangCodeOverride`  
By default, ERPC will use the same language as your game's locale. If you want to use a different language for the rich presence display.
- `UpdateTicksInterval`  
Amount of ticks in between each rich presence update, by default ERPC will wait 8 ticks.

## Customization

To modify or add more variants to the presence phrases, navigate to the `Elin\BepInEx\config\` folder and edit the latest `erpc_localization_*.json` file.

Do not edit the base file in the mod folder, as this file will be overwritten each time the mod updates.

## Translation

Japanese translations are provided by DeepL. If you'd like to improve them or add support for other languages, please let me know.

[source](https://github.com/gottyduke/Elin.Plugins/tree/master/ElinRichPresence)
