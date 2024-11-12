using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Unity.Bootstrap;
using HarmonyLib;

namespace PL;

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