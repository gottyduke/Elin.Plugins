# Elin.PluginTemplate

English | [中文](README.zh.md) | [日本語](README.ja.md)

![nuget](https://img.shields.io/nuget/v/ElinPluginTemplate)
[![.NET SDK 10.0.x](https://img.shields.io/badge/10-green?logoColor=blue&label=dotnet%20SDK&labelColor=blue)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

A `dotnet new` template for quickly scaffolding [Elin](https://store.steampowered.com/app/2135150/Elin/) mod projects using [BepInEx](https://github.com/BepInEx/BepInEx) + [Harmony](https://github.com/pardeike/Harmony).

---

## Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) (or later)
- Elin game installed (Steam)

---

## Install the Template

```bash
dotnet new install ElinPluginTemplate
```

To update an already-installed version:

```bash
dotnet new install ElinPluginTemplate --force
```

---

## Create a New Mod

### Via IDE (Recommended)

Use **JetBrains Rider** or **Visual Studio** — select the **Elin Plugin** template, then fill in the required parameters in the advanced settings dialog.

![new project dialog](./new_project.png)

### Via CLI

```pwsh
dotnet new elinplugin -n MyNewMod --Guid "unique.mod.id" --ModName "My New Awesome Mod"
```

| Parameter  | Description |
|------------|-------------|
| `-n`       | Project / folder name |
| `--Guid`   | Unique mod identifier (e.g. `com.yourname.mod`) |
| `--ModName`| Human-readable mod display name |

---

## Set the Game Path

This template targets Windows and requires the `ElinGamePath` environment variable, set to the root folder of your Elin installation:

```
ElinGamePath/
├─ BepInEx/
│  └─ core/
│     └─ *.dll
└─ Elin_Data/
   └─ Managed/
      └─ *.dll
```

You can set a environment variable by searching "environment" in Start menu and edit from there.

## Build

```pwsh
dotnet build
```

The compiled mod is automatically copied to:

```
{ElinGamePath}\Package\Mod_{ModName}\
```

---

## Project Structure

```
MyNewMod/
├─ Plugin.cs          ← Entry point (BepInEx plugin)
├─ AsmInfo.cs         ← Assembly metadata
├─ package/           ← Assets to bundle with the mod
│  ├─ package.xml
│  ├─ preview.jpg
|  ├─ LangMod/
│  ├─ Texture/
│  └─ Sound/
└─ MyNewMod.csproj    ← .NET project file
```

Everything inside `package/` is copied to the output folder on build. You can include:

- `package.xml` — mod metadata for the Steam Workshop
- `preview.jpg` — preview image
- `LangMod` - source data sheets
- `Texture/` — custom textures
- `Sound/` — custom audio
- …and any other assets your mod needs

---

## Quick Start

```pwsh
# 1. Install the template
dotnet new install ElinPluginTemplate

# 2. Create a new mod
dotnet new elinplugin -n MyMod --Guid "com.elinplugins.myid" --ModName "My Test Mod"

# 3. Build it
cd MyMod
dotnet build
```

Your mod is now in `ElinGamePath/Package/Mod_MyMod/`. Launch Elin and it should load automatically via BepInEx.

---

## License

This template is provided as-is. See the [Elin modding guidelines](https://elin-modding.net/) for community resources.
