using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using HarmonyLib;
using ReflexCLI;

namespace Cwl.Patches.ReflexConsole;

[HarmonyPatch]
internal class GreedyArgumentPatch
{
    private static readonly Dictionary<string, bool> _queriedCommands = [];

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Utils), nameof(Utils.GetCommandTerms))]
    internal static void OnGetGreedyArgument(string commandStr, ref string[] __result)
    {
        var command = __result.TryGet(0, true);
        if (command is null) {
            return;
        }

        var commandDef = CommandRegistry.GetCommand(command);
        if (commandDef.Command?.Member is not { } func) {
            return;
        }

        if (!_queriedCommands.TryGetValue(command, out var greedy)) {
            var attributes = func.GetCustomAttributes(true);
            greedy = _queriedCommands[command] = attributes.Any(attr => attr is DescriptionAttribute {
                Description: "reflex_greedy_args",
            });
        }

        if (greedy) {
            __result = [command, commandStr[command.Length..].Trim()];
        }
    }
}