## Expanded Moongate BepInEx

The base module for Elin that serves as UI and client.

## Prerequisites

Install [.NET SDK 10.0.x](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

Set the following environment variables before building:

- `ElinGamePath` (**required**): Path to the Elin game installation directory.
- `SteamContentPath` (**optional**): Path to your Steam Workshop content directory.

---

## Dependency

### Custom Whatever Loader

The build looks for `CustomWhateverLoader.dll` in this order:

1. `$(SteamContentPath)\2135150\3370512305\CustomWhateverLoader.dll`
2. `$(SteamContentPath)\2135150\3544010094\CustomWhateverLoader.dll`
3. `$(ElinGamePath)\Package\Mod_CustomWhateverLoader\CustomWhateverLoader.dll`
   (used when building Custom Whatever Loader from source in the same solution)

### YK Framework

The build looks for `YKFramework.dll` at:

1. `$(ElinGamePath)\2135150\3400020753\YKFramework.dll`
2. `$(SteamContentPath)\2135150\3400020753\YKFramework.dll`

---

## Build

Clone the repository:

```ps
git clone https://github.com/gottyduke/Elin.Plugins.git
cd Elin.Plugins
```

Restore dependencies:

```powershell
dotnet restore ./ExpandedMoongate/ExpandedMoongate --locked-mode
```

Build:

```powershell
dotnet build ./ExpandedMoongate/ExpandedMoongate -c Debug -o ./out --no-restore
```

> Or open the solution in the IDE and build from there.