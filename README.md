Elin plugins for funzie

## Releases
- [Lose Karma On Caught](./KarmaOnCaught/)
- [Fixed Package Loader](./FixedPackageLoader/)
- [Elin Rich Presence](./ElinRichPresence/)
- [Compare Equipment](./EquipmentComparison/)
- [Variable Sprite Support](./VariableSpriteSupport/)
- [Custom Whatever Loader](./CustomWhateverLoader/)
- [Animated Custom Sprites](./AnimatedCustomSprites/)
- [Mod Viewer Plus](./ModViewerPlus/)
- [Visual PCC Picker](./CharacterCustomizerPlus/)

## Build
The projects require environment variable `ElinGamePath` set to the root folder of the Elin game installation.
```
ElinGamePath/
├─ BepInEx/
│  ├─ core/
│  │  ├─ *.dll
├─ Elin_Data/
│  ├─ Managed/
│  │  ├─ *.dll
```

To build Custom Whatever Loader, you need to install [.NET SDK 10.0.x](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

The regular bugfixes and updates before tagged release can also be downloaded from [action artifacts](https://github.com/gottyduke/Elin.Plugins/actions).

![](https://github.com/gottyduke/Elin.Plugins/actions/workflows/cwl_ci.yml/badge.svg)

Clone the project:
```ps
git clone https://github.com/gottyduke/Elin.Plugins.git
cd Elin.Plugins
```

Install the deps:
```ps
dotnet restore ./CustomWhateverLoader --locked-mode
```

Build the project:
```ps
dotnet build ./CustomWhateverLoader -c Debug -o ./out --no-restore
dotnet build ./CustomWhateverLoader -c DebugNightly -o ./out --no-restore
```

---
<p align="center">MIT License, 2024-present DK</p>
