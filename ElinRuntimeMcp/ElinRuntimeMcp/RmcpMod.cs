using System.IO;
using BepInEx;
using BepInEx.Logging;
using ReflexCLI;
using ReflexCLI.Attributes;
using UnityEngine;

namespace ElinRuntimeMcp;

internal static class ModInfo
{
    internal const string Guid = "dk.elinplugins.runtimemcp";
    internal const string Name = "Elin Runtime MCP";
    internal const string Version = "1.0.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal sealed class RmcpMod : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;

    internal static string ModDir => Path.GetDirectoryName(typeof(RmcpMod).Assembly.Location)!.Replace('\\', '/');

    internal static string BridgePath => $"{ModDir}/bridge/elin-mcp.mjs";

    private void Awake()
    {
        Log = Logger;
        CommandRegistry.assemblies.Add(typeof(RmcpMod).Assembly);

        var enabled = Config.Bind(
            "Listener",
            "Enabled",
            true,
            "Open the localhost port the MCP bridge talks to.\n" +
            "It runs arbitrary C# sent by any local process: keep this mod for development only.");

        if (!enabled.Value) {
            Log.LogInfo("listener disabled in config");
            return;
        }

        RmcpLogBuffer.Install();
        gameObject.AddComponent<RmcpListener>();
        Log.LogInfo($"bridge for your MCP client: node \"{BridgePath}\" (console: rmcp.setup)");
    }
}

[ConsoleCommandClassCustomizer("rmcp")]
internal static class RmcpConsole
{
    /// <summary>
    ///     Print the MCP client configuration for this install and copy it to the clipboard
    /// </summary>
    [ConsoleCommand("setup")]
    internal static string Setup()
    {
        var snippet =
            "{\n" +
            "  \"mcpServers\": {\n" +
            "    \"elin-runtime\": {\n" +
            "      \"type\": \"stdio\",\n" +
            "      \"command\": \"node\",\n" +
            $"      \"args\": [\"{BridgePathJson}\"]\n" +
            "    }\n" +
            "  }\n" +
            "}";
        GUIUtility.systemCopyBuffer = snippet;

        return "copied to clipboard, put it in your mod project's .mcp.json:\n" +
               snippet + "\n" +
               "or for Claude Code:\n" +
               $"claude mcp add elin-runtime -- node \"{RmcpMod.BridgePath}\"\n" +
               "configs for other clients (VS Code, Codex, OpenCode, Cursor...):\n" +
               $"node \"{RmcpMod.BridgePath}\" --config\n" +
               $"agent guide: {RmcpMod.ModDir}/README.md";
    }

    private static string BridgePathJson => RmcpMod.BridgePath.Replace("\"", "\\\"");
}
