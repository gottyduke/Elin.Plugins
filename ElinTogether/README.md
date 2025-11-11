# Eternal League of Networking (EMP)

A WIP attempt of bringing networking feature to Elin.

## Timeline

```mermaid
timeline
    title Elin Net Development (Prototyping)
    10/30/2025 : Initial concept, looked into LiteNetLib
    10/31/2025 : First version of ElinNetBase components
    : Setup NAT holepunch server on GCP
    11/03/2025 : 1st session (Han), NAT holepunch is unreliable
    : Considering SteamNetworkingSockets
    11/07/2025 : Swapped to SteamNetworkingSockets + removed NAT code
    : 2nd session (Han), couldn't beat Puppy Cave due to map loading issue
    11/08/2025 : 3rd session (Omega, Ryozu), testing SteamNetworkingSockets
    : 4th session (InuiDame), testing with high latency
    11/09/2025 : Added delta packets + tick scheduler
    : 5th session (Han), couldn't beat Puppy Cave due to map loading issue
    : Publicized source code
    11/11/2025 : Added shared speed + shared ticking
    : 6th session (Han), couldn't beat Puppy Cave due to level loading issue
```

## Build
This project requires environment variable `ElinGamePath` set to the root folder of the Elin game installation.
```
ElinGamePath/
├─ BepInEx/
│  ├─ core/
│  │  ├─ *.dll
├─ Elin_Data/
│  ├─ Managed/
│  │  ├─ *.dll
```

This project references Custom Whatever Loader, you can get it from Steam Workshop or GitHub tagged releases, either build is fine (as of 1.20.55).

To build EMP, you need to install [.NET SDK 10.0.x](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

Clone the project:
```ps
git clone https://github.com/gottyduke/Elin.Plugins.git
cd Elin.Plugins
```

Install the deps:
```ps
dotnet restore ./ElinTogether/ElinTogether --locked-mode
```

Build the project:
```ps
dotnet build ./ElinTogether/ElinTogether -c Debug -o ./out --no-restore
```

---
<p align="center">MIT License, 2024-present DK</p>