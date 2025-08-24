# Elin.PluginTemplate

## Consumption

JetBrains Rider, Visual Studio, or if you really want to use VSC and have no interest in debugging.

Optionally you can install [.NET SDK 10.0.x](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

## Build

The environment variable `ElinGamePath` must be set to the root path of the Elin installation. 
```
ElinGamePath/
├─ BepInEx/
│  ├─ core/
│  │  ├─ *.dll
├─ Elin_Data/
│  ├─ Managed/
│  │  ├─ *.dll
```

The template contains placeholder root namespace name and assembly name, change it before compiling.

Anything put inside `package/` folder will be copied to the output folder, which is `ElinGamePath/Package/Mod_AssemblyName/`.