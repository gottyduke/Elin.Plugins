using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Cwl.Helper.String;
using HarmonyLib;
using ReflexCLI;

namespace Cwl.Patches.ReflexConsole;

[HarmonyPatch]
internal class ReflexConsoleSupplementPatch
{
    private static readonly Dictionary<string, ReflexCommandSupplement> _queriedCommands = [];

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

        if (!_queriedCommands.TryGetValue(command, out var data)) {
            var attributes = func.GetCustomAttributes(true);
            var desc = attributes.OfType<DescriptionAttribute>().FirstOrDefault();
            data = _queriedCommands[command] = new(desc?.Description);
        }

        if (data.IsGreedy) {
            __result = [command, commandStr[command.Length..].Trim()];
        }
    }

    private class ReflexCommandSupplement
    {
        public readonly bool IsGreedy;
        //public readonly string Help;

        public ReflexCommandSupplement(string? args)
        {
            if (args.IsEmptyOrNull) {
                IsGreedy = false;
                return;
            }

            if (args.ToLowerInvariant().Contains("reflex_args=greedy")) {
                IsGreedy = true;
            }
        }
    }
}