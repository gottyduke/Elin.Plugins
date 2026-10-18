---
name: elin-runtime
description: Use when testing, debugging or inspecting an Elin mod inside the running game, when tools like elin_instances, elin_eval, elin_hotload or elin_logs are available, when an Elin Harmony patch compiles but has not been seen to fire, or when an Elin mod fails to load or behaves differently in game than expected.
---

# Elin Runtime

The **Elin Runtime MCP** mod runs a listener inside the game, and its `elin-runtime` MCP server turns that into tools. The running game is the ground truth: a change is done when you have watched it work in the game, not when it compiles.

Full reference: `README.md` in the mod folder, also at <https://github.com/gottyduke/Elin.Plugins/tree/master/ElinRuntimeMcp>. For "how do I make X" and for reading the game's source, the companion skill is `elin-modding` (<https://github.com/Elin-Modding-Resources/Elin.MCP/tree/main/skills>).

## Which tools you have

| Server | Tools | If missing |
|---|---|---|
| `elin-runtime` (local, this mod) | `elin_instances`, `elin_eval`, `elin_wait_eval`, `elin_command`, `elin_state`, `elin_screenshot`, `elin_hotload`, `elin_hotunload`, `elin_logs`, `elin_wait_log`, `elin_build`, `elin_launch`, `elin_kill` | The user enables the mod, installs Node.js 20+, adds the entry printed by `node "<mod folder>/bridge/elin-mcp.mjs" --config`, and restarts the client. You cannot add an MCP server to your own running session. |
| `elin` (remote knowledge server, <https://mcp.elin-modding.net>) | `elin_guide`, `elin_find`, `elin_outline`, `elin_callers`, `elin_read`, `elin_strings`, `elin_versions`, … | Optional. The same `--config` output includes it. |

Start with `elin_instances`. If nothing is listening, `elin_launch` starts the game through Steam and waits for it.

## From change to evidence

1. `elin_build` compiles the mod (`project` argument, or `ELIN_BUILD_PROJECT` from the server's env).
2. `elin_kill`, then `elin_launch`. The game keeps the DLL it loaded at startup until it restarts. `elin_hotload` cannot swap in a built DLL; it compiles `.cs` source.
3. Load a save when the code path needs a player or a zone, which is most of them. The title screen is enough for reflection and console commands.
   - `elin_eval`: `JsonConvert.SerializeObject(GameIO.GetGameList(CorePath.RootSave).Select(g => new { g.id, g.pcName, g.zoneName }))`
   - `elin_eval`: `Game.Load("<id>", false); return "loading";`
   - `elin_wait_eval`: `EClass.core.IsGameStarted`
4. Trigger the code path with `elin_eval` or `elin_command`. Then confirm with `elin_wait_log` on a line your code logs, `elin_eval` reading the resulting state, or `elin_screenshot` for anything visual.
5. Run `elin_logs level="Warning" group=true` before reporting success.

## Trying a patch without rebuilding

`elin_hotload file="Probe.cs"` (or `source="..."`) compiles the code with the game's Roslyn. It can use every loaded assembly, including the public types of the user's mod.

```csharp
using HarmonyLib;

public static class Probe
{
    private static readonly Harmony _harmony = new("probe.die");
    public static void EScriptLoad() => _harmony.PatchAll(typeof(Probe).Assembly);
    public static void EScriptUnload() => _harmony.UnpatchSelf();
}

[HarmonyPatch(typeof(Chara), nameof(Chara.Die))]
internal static class ProbeDie
{
    [HarmonyPrefix]
    internal static void Log(Chara __instance) => UnityEngine.Debug.Log($"[probe] Die: {__instance.Name}");
}
```

- To retry after a fix, `elin_hotunload` the old name (it is in `elin_hotload`'s reply), then `elin_hotload` again without `name`. The default name carries a millisecond stamp. Mono cannot unload assemblies, so an explicit name works once per game session.
- To undo a patch, call `UnpatchSelf()` on your own `Harmony` instance. The static `Harmony.UnpatchAll()` strips every mod's patches.
- If `elin_hotload` returns an error from `EScriptLoad`, treat the patch as not applied.

## elin_eval rules

- The script runs on the main thread and the game freezes until it returns. Do not `await`, and do not loop waiting for game state; poll with `elin_wait_eval`.
- A call that times out still finishes in the game, so check state before retrying anything with side effects.
- Qualify game statics: `EClass.pc`, `EClass._zone`, `EClass.game`, `EClass.core`.
- Return `JsonConvert.SerializeObject(...)` for structured results. Anything else non-primitive comes back as `ToString()`.
- Internal types need `AccessTools` or reflection. `Task` is Elin's own AI class; write `System.Threading.Tasks.Task` for the BCL one.
- `Script["key"] = value` keeps a value between calls.

## Looking up game code

This server does not index source. Use these three, in this order:

1. **Knowledge server**, if connected. `elin_guide` answers how to make something. For how something is implemented, use `elin_find`, then `elin_callers`, then `elin_read`. `elin_strings` finds values such as sound ids and lang keys, which are not symbol names.
2. **Reflection in `elin_eval`** gives the exact signatures of the installed version, including inherited overloads and the parameter names Harmony binds by:
   `string.Join("\n", typeof(Chara).GetMethods(AccessTools.all).Where(m => m.Name == "DamageHP").Select(m => m.DeclaringType.Name + "." + m.Name + "(" + string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)) + ")"))`
3. **A local decompile** you make yourself from the installed game, so it matches that version. `<Elin install>` is the game root, the folder with `BepInEx` and `Elin_Data`, and is often in `$ElinGamePath`. You need the .NET 10 SDK.
   ```bash
   dotnet tool install --global ilspycmd
   ilspycmd -p -o ./Elin-Decompiled/Elin -r "<Elin install>/Elin_Data/Managed" -r "<Elin install>/Package/_ModdingKit" -r "<Elin install>/BepInEx/core" "<Elin install>/Elin_Data/Managed/Elin.dll"
   ```
   All game logic is in `Elin.dll`. `Assembly-CSharp.dll` is a near-empty leftover. `Lang` and `BaseModManager` are in `Plugins.BaseCore.dll`, and the official modding API is in `Plugins.Modding.dll`. Decompile those the same way when you need them.

When the knowledge server is connected, compare `elin_versions` with the game version from `elin_instances` (`EA 23.349` vs `23.348.1`: compare the middle number). If the installed game is newer than the index, use reflection for signatures and a fresh decompile for method bodies, and tell the user the index lags.

Locate before you read. `Chara.cs` is about 67k tokens and `Card.cs` about 45k. Search for the member with `rg -n`, then read about 60 lines around the hit.

## When a mod does not load

The loader logs its decisions before this mod starts. Those lines show `startup` instead of a time and have no reliable level, so search them with `elin_logs contains="..."`:

| Search for | Meaning |
|---|---|
| `Failed to activate mod` | Activation threw. The line has the mod title and the reason. |
| `Skipped (` | Made for an older game version, or a dependency is missing. |
| `Blocked:` | Incompatible with another installed mod. |
| `Not a package:` / `No <id> in package.xml` | Missing `package.xml`, or one without `<id>`. |
| `does not parse` | Broken `package.xml`. The game refuses to start. |
| `Activating(` | Activation was attempted. It can still have failed. |

Older stable builds log fewer of these. Then read `Player.log`, which sits next to the game's `Save` folder.

## Common mistakes

| Mistake | Do instead |
|---|---|
| Testing right after `elin_build` | `elin_kill` and `elin_launch` first |
| Patch compiles, so it is reported as working | Trigger it and see its log line or effect |
| Patch loads but never fires | `elin_callers` to check the method runs on that path; reflect to check the overload and parameter names |
| Calling `elin_eval` on player state at the title screen | Load a save first |
| Reading a whole decompiled file | `rg -n` for the member, read a slice |
| Moving the player in the user's main save | Most zone changes autosave. Ask first, or use a throwaway save. |
| `elin_instances` shows `BUSY` | The main thread is loading or running a long eval. Wait, or target by port. `elin_logs` still works. |
