Elin plugins for funzie

## Releases
- [Animated Custom Sprites](./AnimatedCustomSprites/) ![](https://github.com/gottyduke/Elin.Plugins/actions/workflows/acs_ci.yml/badge.svg)
- [Visual PCC Picker](./CharacterCustomizerPlus/)
- [Custom Whatever Loader](./CustomWhateverLoader/) ![](https://github.com/gottyduke/Elin.Plugins/actions/workflows/cwl_ci.yml/badge.svg)
- [Elin Rich Presence](./ElinRichPresence/)
- [Elin Together](./ElinTogether/) ![](https://github.com/gottyduke/Elin.Plugins/actions/workflows/emp_ci.yml/badge.svg)
- [Elin with AI](./Emmersive/) ![](https://github.com/gottyduke/Elin.Plugins/actions/workflows/emmersive_ci.yml/badge.svg)
- [Compare Equipment](./EquipmentComparison/)
- [Expanded Moongate Server](./ExpandedMoongate/) ![](https://github.com/gottyduke/Elin.Plugins/actions/workflows/exmoongate_ci.yml/badge.svg)
- [Fixed Package Loader](./FixedPackageLoader/)
- [Lose Karma On Caught](./KarmaOnCaught/)
- [Mod Viewer Plus](./ModViewerPlus/)
- [Variable Sprite Support](./VariableSpriteSupport/)

## Build

### Requirements
[![.NET SDK 10.0.x](https://img.shields.io/badge/10-green?logoColor=blue&label=dotnet%20SDK&labelColor=blue)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

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

### DotNet Build
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
