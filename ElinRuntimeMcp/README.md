# Elin Runtime MCP

A development mod that lets an AI coding agent drive the running game through the [Model Context Protocol](https://modelcontextprotocol.io). The agent can evaluate C# inside Elin, run console commands, take screenshots, read the BepInEx and Unity log, hot-load a Harmony patch without rebuilding, and build and relaunch the game. It works with any MCP client that can start a stdio server, including Claude Code, Codex, OpenCode, Gemini CLI, Cursor, and VS Code.

This is a tool for modders. It opens a localhost port that executes any C# it receives, so do not keep it enabled for normal play.

> **If you are an AI agent reading this file:** the folder that contains it is the installed mod. If the `elin_*` runtime tools are not connected yet, follow [Quick start](#quick-start). Then read [`skills/elin-runtime/SKILL.md`](skills/elin-runtime/SKILL.md), which is the short working guide. This README is the reference behind it.

| To | Read |
|---|---|
| Connect an MCP client | [Quick start](#quick-start) |
| Give your agent the working rules | [Agent skill](#agent-skill) |
| Test a change, try a patch, find out why a mod did not load | [Workflows](#workflows) |
| Find out how the game implements something | [Looking up game code](#looking-up-game-code) |
| Show your own mod's data in `elin_instances` and `elin_state` | [Extending it from your mod](#extending-it-from-your-mod) |
| Fix a setup problem | [Troubleshooting](#troubleshooting) |
| Build this mod | [Building from source](#building-from-source) |

## Quick start

1. Check that Node.js 20 or newer is installed with `node --version`. Install it from <https://nodejs.org> if needed.
2. Make sure the game has this mod enabled in its mod list. The game does not need to be running yet.
3. Print the client configuration for this install:

   ```
   node "<this folder>/bridge/elin-mcp.mjs" --config
   ```

   It prints ready-to-paste entries for Claude Code, Cursor, Gemini CLI, VS Code, OpenCode, and Codex, with this install's absolute path filled in. The in-game console command `rmcp.setup` prints the same path and copies a `.mcp.json` entry for this server alone to the clipboard.
4. Add the entry to your MCP client and restart the client. MCP servers are only read at startup.
5. Call `elin_instances`. If nothing is listening, call `elin_launch` to start the game through Steam.

"This folder" is the mod folder:

| Install | Folder |
|---|---|
| Steam Workshop | `<Steam library>/steamapps/workshop/content/2135150/<workshop item id>/` |
| Built from source or copied by hand | `<Elin install>/Package/Mod_ElinRuntimeMcp/` |

If you cannot run a command, write the entry by hand. It is a stdio server started with `node` and the bridge's absolute path. In the `mcpServers` format of Claude Code and Cursor it looks like this:

```json
{
  "mcpServers": {
    "elin-runtime": { "command": "node", "args": ["<this folder>/bridge/elin-mcp.mjs"] },
    "elin": { "type": "http", "url": "https://mcp.elin-modding.net" }
  }
}
```

The second entry, `elin`, is a companion knowledge server that indexes the game's source and the modding wiki. It is optional but recommended (see [Looking up game code](#looking-up-game-code)).

To give `elin_build` a default target, add environment variables to the `elin-runtime` entry. The key is `env` in most clients and `environment` in OpenCode:

```json
"env": { "ELIN_BUILD_PROJECT": "MyMod/MyMod.csproj", "ELIN_BUILD_CONFIGURATION": "Debug" }
```

The project path is relative to the directory the MCP client starts the server in, which is normally your project root.

## Agent skill

[`skills/elin-runtime/SKILL.md`](skills/elin-runtime/SKILL.md) is an [Agent Skill](https://agentskills.io). It tells an agent how to go from a code change to evidence in the game, how to keep `elin_eval` from freezing the game, how to try and undo a patch, where to look up game code, and which log lines explain a mod that did not load. Clients that support skills load it only when a task calls for it.

Copy the `elin-runtime` folder into your client's skills folder, or link it there so Workshop updates reach your agent:

| Client | Skills folder |
|---|---|
| Claude Code | `.claude/skills/` in your project, or `~/.claude/skills/` for every project |
| Codex, Gemini CLI, GitHub Copilot CLI | `~/.agents/skills/` |
| Others | See the client's documentation. A client without skill support can be told to read the file. |

```powershell
# Windows: a junction needs no admin rights
New-Item -ItemType Junction -Path "$HOME\.claude\skills\elin-runtime" -Target "<this folder>\skills\elin-runtime"
```

```bash
ln -s "<this folder>/skills/elin-runtime" ~/.claude/skills/elin-runtime
```

The companion skill [`elin-modding`](https://github.com/Elin-Modding-Resources/Elin.MCP/tree/main/skills) covers the knowledge server, mod packaging, and decompiling the game. Install both if you use both servers.

## Tools

| Tool | What it does |
|---|---|
| `elin_instances` | Lists running instances with port, pid, scene, game version, and any fields other mods add |
| `elin_eval` | Evaluates a C# script on the Unity main thread, using the game's own Roslyn |
| `elin_wait_eval` | Polls a C# expression until it is truthy, treating runtime exceptions as false |
| `elin_command` | Runs an in-game console command, such as `mod.elin_dev true` or anything a mod registers |
| `elin_state` | Returns scene, zone, and player as JSON, plus sections other mods add |
| `elin_screenshot` | Captures the frame, saves the PNG, and returns it inline unless `inline=false` |
| `elin_hotload` / `elin_hotunload` | Compiles a `.cs` file or a source string in the game and loads it as a throwaway assembly |
| `elin_logs` | Filters, tails, or groups the in-game log buffer |
| `elin_wait_log` | Waits for a log line that matches a regex |
| `elin_build` | Runs `dotnet build` and returns only the error and warning lines |
| `elin_launch` | Starts the game through Steam, optionally inside a Sandboxie-Plus box |
| `elin_kill` | Terminates one instance or all of them |

Every tool that talks to the game takes an optional `instance` argument, described in [Several instances](#several-instances).

## Workflows

### Check a change to your mod

1. `elin_build` compiles the mod.
2. `elin_kill`, then `elin_launch`. The game does not lock mod DLLs, so building while it runs works, but the running game keeps the old code until it restarts.
3. Load a save if the change needs one (see [Recipes](#recipes)).
4. Trigger the code path with `elin_eval` or `elin_command`. Confirm it with `elin_wait_log`, `elin_eval`, or `elin_screenshot`.
5. `elin_logs level="Warning" group=true` shows every repeated warning and error at a glance.

Most zone changes autosave. Moving the player around in someone's main save changes that save, so use a throwaway save for tests.

### Try a patch without rebuilding

`elin_hotload` compiles a `.cs` file, or source passed as a string, with the game's Roslyn. It references every assembly the game has loaded, including your mod's public types. A static `EScriptLoad()` runs after loading and `EScriptUnload()` runs on `elin_hotunload`:

```csharp
using HarmonyLib;

public static class TryFix
{
    private static readonly Harmony _harmony = new("try.fix");

    public static void EScriptLoad() => _harmony.PatchAll(typeof(TryFix).Assembly);
    public static void EScriptUnload() => _harmony.UnpatchSelf();
}

[HarmonyPatch(typeof(Chara), nameof(Chara.Die))]
internal static class DiePatch
{
    [HarmonyPrefix]
    internal static void Log(Chara __instance) => UnityEngine.Debug.Log($"dying: {__instance.Name}");
}
```

Undo a patch with the instance method `UnpatchSelf()`. Never call the static `Harmony.UnpatchAll()`, because it removes the patches of every mod in the game.

If `EScriptLoad()` throws, `elin_hotload` returns an error with the real exception and stack trace, and the patch is probably not applied. Compile errors come back with line numbers.

Mono cannot unload assemblies, so every load stays in memory until the game restarts, even after `elin_hotunload`. Loading a name that is already loaded is refused. To try a fixed version, `elin_hotunload` the old one and load again without `name`: the default name carries a millisecond timestamp. `elin_hotunload` only accepts names that `elin_hotload` loaded in the current game session.

`elin_hotload` takes source, not a built DLL. To test your built mod, restart the game.

### Find out why a mod did not load

Search `elin_logs` with `contains` for these strings. The mod loader logs them before this mod loads, so they come from the startup copy of Unity's `Player.log` and show `startup` instead of a time.

| Search for | Meaning |
|---|---|
| `Failed to activate mod` | The mod's activation threw. The line carries the mod title and the reason. |
| `Skipped (` | Skipped because it was made for an older game version, or because a dependency is missing. The line names which. |
| `Blocked:` | Skipped because it is incompatible with another installed mod. |
| `Not a package:` | A folder without a `package.xml`. The line gives the path. |
| `does not parse` | A broken `package.xml`. In that case the game refuses to start, and `#mod game will not start` follows. |
| `No <id> in package.xml` | A `package.xml` without an `<id>`. The line gives the path. |
| `has failed to initialize` | Reading the package failed before activation. |
| `Activating(` | Activation was attempted, in load order. A mod listed here can still have failed. |

Older stable builds of the game log fewer of these lines. In that case read `Player.log` next to the game's `Save` folder.

## elin_eval reference

- The game is frozen while a script runs. Do not `await`, and do not loop waiting for game state inside a script. Use `elin_wait_eval` to poll instead.
- A call that times out on the client side still runs to completion in the game, so do not blindly retry scripts with side effects.
- The last expression is the result, or use `return x;`.
- Strings, bools, and numbers come back as they are. Integers beyond 2^53, decimals, and anything else come back as text, so serialize structure yourself with `JsonConvert.SerializeObject(...)`.
- Game statics must be qualified: `EClass.pc`, `EClass._zone`, `EClass.game`, `EClass.core`.
- Default usings are System, System.Linq, System.Collections.Generic, System.IO, System.Reflection, UnityEngine, HarmonyLib, Newtonsoft.Json, and EModding.
- `Task` resolves to the game's own AI class `Task`. Write `System.Threading.Tasks.Task` when you mean the BCL type.
- Public types of any loaded mod work by full name. Internal ones need `AccessTools` and reflection.
- `Script["key"] = value` persists across calls. Values are `object`, so cast when reading them back.
- Compile errors come back with line numbers, and runtime exceptions come back with stack traces.

### Recipes

List saves, then load one:

```csharp
JsonConvert.SerializeObject(GameIO.GetGameList(CorePath.RootSave).Select(g => new { g.id, g.pcName, g.zoneName }))
```

```csharp
Game.Load("<id>", false); return "loading";
```

Then `elin_wait_eval` with `EClass.core.IsGameStarted`.

Get the exact signatures of a method in the installed game version, including overloads declared on base classes and the parameter names that Harmony injects by:

```csharp
string.Join("\n", typeof(Chara).GetMethods(AccessTools.all).Where(m => m.Name == "DamageHP").Select(m => m.DeclaringType.Name + "." + m.Name + "(" + string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)) + ")"))
```

Get a snapshot of the player:

```csharp
JsonConvert.SerializeObject(new { EClass.pc.Name, EClass.pc.hp, EClass.pc.MaxHP, zone = EClass._zone.Name, x = EClass.pc.pos.x, z = EClass.pc.pos.z })
```

Turn on the game's debug cheats without restarting with `elin_command "mod.elin_dev true"`.

## Looking up game code

This server runs code in the game but does not index its source. Use these three sources, in this order:

| Source | Answers | Needs |
|---|---|---|
| [Knowledge server](#the-knowledge-server) | How do I make X? Where is X implemented, and who calls it? Which ids and sheet columns exist? | Nothing local, only the URL |
| [Reflection](#reflection-in-the-running-game) in `elin_eval` | The exact signatures, overloads, and parameter names in the version you have installed | The game running with this mod |
| [A local decompile](#a-local-decompile) | Method bodies of a game version the index does not have yet, regex across the whole source, offline work | .NET 10 SDK and `ilspycmd` |

### The knowledge server

<https://mcp.elin-modding.net> serves the decompiled game source and the modding wiki over MCP, with no install and no game files. The `--config` output includes it as `elin`. It indexes Elin's two branches, `stable` and `nightly`.

| For | Call |
|---|---|
| How do I make X | `elin_guide`, then the articles it names |
| Where X is implemented | `elin_find`, then `elin_read` for the one member you need |
| When X runs | `elin_callers`. Skipping this is the usual reason a patch compiles, loads, and never fires. |
| A value rather than a name, such as a sound id or a lang key | `elin_strings` |
| Which game version each branch is indexed at | `elin_versions` |

Compare `elin_versions` with the game version that `elin_instances` reports. `EA 23.349` against `23.348.1` means compare the middle number; the third part is a hotfix counter. When the installed game is newer than the index, use reflection for signatures and a local decompile for method bodies.

### Reflection in the running game

The signature recipe under [Recipes](#recipes) answers for the version that is actually running, including overloads declared on base classes. It matters most on the nightly branch, where the index can lag behind.

### A local decompile

Make your own copy from the game you have installed, so it always matches that version. The modding kit references Roslyn in its own folder and Harmony in `BepInEx/core`, so pass all three folders with `-r`, or signatures show bare assembly names instead of types.

```bash
dotnet tool install --global ilspycmd
```

PowerShell:

```powershell
$Game = "C:\Program Files (x86)\Steam\steamapps\common\Elin"   # or $env:ElinGamePath
$M = "$Game\Elin_Data\Managed"
foreach ($a in 'Elin', 'Plugins.BaseCore', 'Plugins.Modding') {
    ilspycmd -p -o ".\Elin-Decompiled\$a" -r $M -r "$Game\Package\_ModdingKit" -r "$Game\BepInEx\core" "$M\$a.dll"
}
```

bash:

```bash
GAME="/c/Program Files (x86)/Steam/steamapps/common/Elin"   # or "$ElinGamePath"
M="$GAME/Elin_Data/Managed"
for a in Elin Plugins.BaseCore Plugins.Modding; do
  ilspycmd -p -o "./Elin-Decompiled/$a" -r "$M" -r "$GAME/Package/_ModdingKit" -r "$GAME/BepInEx/core" "$M/$a.dll"
done
```

| Assembly | Contents |
|---|---|
| `Elin` | All game logic. This is what Harmony patches target. |
| `Plugins.BaseCore` | `Lang`, `EInput`, and `BaseModManager`, the mod event bus |
| `Plugins.Modding` | The official modding API: `EMod`, `ICustomContent`, `PackageIterator` |

`Assembly-CSharp.dll` is a near-empty leftover, so skip it. The `elin-modding` skill lists the full set of assemblies the knowledge server indexes. Elin is a Mono build, so there is no IL2CPP step.

Keep the output out of your mod's repository and do not publish it. It is for reading.

Do not read a decompiled file whole. `Chara.cs` is 248 KB and `Card.cs` is 167 KB, which is about 67k and 45k tokens. Locate the member first, then read only its lines:

```bash
rg -n "\bvoid Die\(" Elin-Decompiled/Elin/Chara.cs   # the member's line
rg -n "\.Die\(" Elin-Decompiled -g "*.cs"            # call sites
```

Search by name. Line numbers from the knowledge server match your copy only when both were made with the same `ilspycmd` version.

## Several instances

Each instance takes the first free port in 27551-27560, and `instance` selects between them:

- `any` selects the only running instance. This is the default.
- A number in the port range is always a port. Any other number is a pid.
- Any other word matches a value that a mod added to `hello`. A multiplayer mod that reports `role = Host` lets you write `instance: "host"`.

`elin_instances` marks an instance whose main thread did not answer, because it is loading or running a long eval, as `BUSY`. It marks one whose `hello` failed as `ERROR`. Both still show their pid and can be targeted by port. `elin_logs`, `elin_wait_log`, and `elin_kill` keep working in that state because they do not need the main thread.

`elin_launch sandbox=<box>` starts another copy inside a Sandboxie-Plus box. It finds `steam.exe` through the `SteamPath` environment variable or the registry, and `Start.exe` through `SandboxPath` or the default install folder.

## Extending it from your mod

Two events on the game's own mod event bus let any mod add information without referencing this one. Both carry an `IDictionary<string, object>` that you fill in:

```csharp
BaseModManager.SubscribeEvent<IDictionary<string, object?>>("runtimemcp.hello", hello => hello["role"] = MyNet.Role);
BaseModManager.SubscribeEvent<IDictionary<string, object?>>("runtimemcp.state", state => state["mymod"] = new { MyMod.Counter });
```

- `elin_instances` shows the fields you add to `hello`, and their string values work as instance selectors.
- `elin_state` returns the sections you add to `state`.
- Values are serialized with Newtonsoft.Json, one key at a time. Unity types such as `Vector3` or `GameObject` are sent as their `ToString()` text. A value nested deeper than 6 levels, such as a whole `Chara`, or one that cannot be serialized, shows up as an `<unserializable ...>` placeholder instead of breaking the reply. Integers beyond 2^53 are sent as text so that ids like SteamIDs survive. Keep values to plain data.
- Built-in fields win on a name clash.
- If this mod is not installed, nothing publishes these events and your handlers never run.

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| `elin_instances` finds nothing while the game runs | The mod is disabled in the mod list, or `Enabled = false` under `[Listener]` in `BepInEx/config/dk.elinplugins.runtimemcp.cfg`. The game log says `listening on 127.0.0.1:<port>` when the listener is up. |
| The game does not start after installing | Look for `does not parse` in `Player.log`. A mod with a broken `package.xml` stops the game from starting. |
| The MCP client shows no `elin_*` tools | The client was not restarted after the config change, or `node` is not on its PATH. Run `node "<bridge path>" --help` in a terminal to check. |
| An instance shows `BUSY` | Its main thread is blocked by loading or a long eval. Wait, or target it by port. |
| Every instance turns `BUSY` when the game window loses focus | The game's "run in background" option is off. Turn it on in the game's settings. |
| Unity `Debug.Log` lines are missing from `elin_logs` | `UnityLogListening` is off in `BepInEx/config/BepInEx.cfg`. |
| `elin_hotload` refuses a name | That assembly name is already loaded. Load again without `name`, or pick a new one. |
| `no free port in 27551-27560` in the game log | Ten instances are already running, or other programs hold those ports. |

## Wire protocol

The in-game listener speaks newline-delimited JSON, one request per line:

```
→ {"id":1,"op":"eval","args":{"code":"EClass.pc.Name"}}
← {"id":1,"ok":true,"result":"Chris","ms":4}
← {"id":1,"ok":false,"error":"EScriptCompilationException: ...","trace":"...","ms":2}
```

| Op | Args |
|---|---|
| `hello`, `state`, `ping` | none |
| `command` | `cmd` |
| `eval` | `code` |
| `screenshot` | `scale` |
| `load` | `source`, `file`, `name` |
| `unload` | `name` |
| `log` | `after`, `limit` |

`log` and `ping` are answered from the socket thread. Every other op runs on the main thread. A connection whose first line does not start with `{` is closed without a reply, so a browser cannot smuggle a request in an HTTP body.

## Security

The port binds to 127.0.0.1 only, but any local process can run code in the game through it. To turn it off, set `Enabled = false` under `[Listener]` in `BepInEx/config/dk.elinplugins.runtimemcp.cfg`, or unsubscribe when you are not developing.

## Building from source

```powershell
cd ElinRuntimeMcp/bridge
pnpm install          # once; afterwards every dotnet build regenerates the bundle
cd ../ElinRuntimeMcp
dotnet build          # outputs to $(ElinGamePath)/Package/Mod_ElinRuntimeMcp
```

The build copies `package/`, this README, and `skills/` into the mod folder. `package/bridge/elin-mcp.mjs` and its `THIRD-PARTY-NOTICES.txt` are generated from `bridge/src` and committed, so CI and packaging do not need Node. Run `pnpm check` in `bridge/` to type-check. Release builds leave out the pdb.

CI (`.github/workflows/rmcp_ci.yml`) builds Debug and Release against both the stable and nightly game branches on every push that touches this folder, except `bridge/` alone, and publishes the zips under the GitHub release tag `RMCP-<version>`. The version comes from `ModInfo.Version` in `RmcpMod.cs`.
