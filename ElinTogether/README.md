# Eternal League of Networking (EMP)

A WIP attempt of bringing networking feature to Elin.

## Play
To play with friends, it's recommended to use a minimal modlist and keep them consistent for all players. Use steam workshop collections for that purpose.

Host needs to start the game, load into a save or make a new game, and open up the panel from Esc-Mods-Elin Together.

From there, host can invite friends directly from panel or right click in steam friend list and Invite to Game.

The player characters are randomly generated right now(for testing purpose)
.

## Timeline

### Prototyping
```mermaid
timeline
    10/30/2025 : Initial concept, looked into LiteNetLib
    10/31/2025 : First version of ElinNetBase components
    : Setup NAT holepunch server on GCP
    11/03/2025 : Session #1 (Han), NAT holepunch is unreliable
    : Considering SteamNetworkingSockets
    11/07/2025 : Swapped to SteamNetworkingSockets + removed NAT code
    : Session #2 (Han), couldn't beat Puppy Cave due to map loading issue
    11/08/2025 : Session #3 (Omega, Ryozu), testing SteamNetworkingSockets
    : Session #4 (InuiDame), testing with high latency
    11/09/2025 : Added delta packets + tick scheduler
    : Session #5 (Han), couldn't beat Puppy Cave due to map loading issue
    : Publicized source code
```

### Puppy Caving
```mermaid
timeline
    11/11/2025 : Added shared speed + shared ticking
    : Session #6 (Han), couldn't beat Puppy Cave due to level loading issue
    : Session #7 (Omega), couldn't beat Puppy Cave due to 3rd player not synced
    11/12/2025 : Session #8 (InuiDame), couldn't beat Puppy Cave due to not handling client death
    : Session #9 (Han, Omega), 3 player testing, couldn't beat Puppy Cave due to enemies not taking death updates
    : Session #10 (Drakeny), couldn't beat Puppy Cave due to Drakeny died so fast and there's no host death handling code
    11/13/2025 : Rewrote entire zone loading system, added Act delta
    : Session #11 (Han, Puddles), 3 player testing, Han was riding Puddles. Be submissive
    11/14/2025 : Added steam lobby browser and friend invites
    : Session #12 (Han), couldn't beat Puppy Cave due to having a ghost character(renderer?)
    : Session #13 (105gun), the hand is here. Couldn't beat puppy cave due to death desyncs
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
