As of Elin `23.24 fix 4`, there's currently a bug regarding Elin's `PackageChainloader` used with `BepInEx 6.0.0-pre.1`. The chainloader is unable to resolve any assembly dependencies in the new paths (`Packages\<Mod>` for local mods and `2135150\<ModId>` for workshop mods).

### To replicate the error:
1. Create a script mod with an additional DLL dependency (e.g., via a NuGet package).
2. Place the mod in either the `Packages` folder or the `2135150` workshop folder.

The mod will always fail to load, despite the dependencies being placed in the same folder. However, the same mod setup can be successfully loaded from the `BepInEx\plugins` folder, which utilizes `BepInEx`'s own `BaseChainloader`.

### The cause:
`BepInEx 6.0.0-pre.1` uses `Assembly.LoadFile` internally, and when Elin's `PackageChainloader` attempts to load plugins from the new path (`NewPath`), `Assembly.LoadFile` instantiates a new `AssemblyLoadContext` that is different from the default assembly resolver algorithm, thereby failing to lookup any additional dependencies.

### The fix:
1. Upgrade to `BepInEx 6.0.0-pre.2`, which uses `Assembly.LoadFrom`, that will load plugins with `AssemblyLoadContext.Default` and a handler that will load the assembly's dependencies from its directory. **However**, this will cause all current script mods to break due to the framework change.
2. (My temp fix) Patch `BepInEx.Core` as early as possible, before loading any other script mods. I have implemented this as an Elin mod with a `loadPriority` set to `-99`. Or directly implement this in the Elin's `PackageChainloader`.

```cs
[HarmonyPatch]
internal class AssemblyLoadContextPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BaseChainloader<BaseUnityPlugin>), "LoadPlugins", [typeof(IList<PluginInfo>)])]
    internal static IEnumerable<CodeInstruction> OnLoadPluginsIl(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false, 
                new CodeMatch(OpCodes.Call, AccessTools.Method(
                    typeof(Assembly), 
                    nameof(Assembly.LoadFile), 
                    [typeof(string)])))
            .SetOperandAndAdvance(AccessTools.Method(
                typeof(Assembly), 
                nameof(Assembly.LoadFrom),
                [typeof(string)]))
            .InstructionEnumeration();
    }
}
```

### For references:
[`BepInEx 6.0.0-pre.1` `Assembly.LoadFile`](https://github.com/BepInEx/BepInEx/blob/ec79ad057b20c302c17b34e63906ee398352d852/BepInEx.Core/Bootstrap/BaseChainloader.cs#L396)

[`BepInEx 6.0.0-pre.2` `Assembly.LoadFrom`](https://github.com/BepInEx/BepInEx/blob/e1974e26fd7702c66b54c0d6879c90b988cc4920/BepInEx.Core/Bootstrap/BaseChainloader.cs#L407)