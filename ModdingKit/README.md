# Elin Scripting Kit

Source codes of the Scripting Kit, ported from CustomWhateverLoader.

## Build

### Requirements
[![.NET SDK 10.x](https://img.shields.io/badge/10-green?logoColor=blue&label=dotnet%20SDK&labelColor=blue)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

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
cd ./Elin.Plugins/ModdingKit/
```

Install the deps:
```ps
dotnet restore ./ModdingKit --locked-mode
```

Build the project:
```ps
dotnet build ./ModdingKit -c Debug -o ./out --no-restore
```
