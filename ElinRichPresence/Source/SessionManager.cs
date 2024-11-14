using DiscordRPC;
using DiscordRPC.Logging;
using Erpc.Resources;

namespace Erpc;

internal class SessionManager(string appId)
{
    private readonly DiscordRpcClient _client = new(appId);
    private readonly Timestamps _sessionTime = new() { Start = DateTime.UtcNow };
    private bool IsReady { get; set; }

    internal void Initialize()
    {
        _client.Logger = new ConsoleLogger { Level = LogLevel.Error };
        _client.OnReady += (_, _) => {
            ErpcMod.Log("rpc pipe is ready");
            IsReady = true;
        };
        _client.Initialize();
    }

    internal void Update(RichPresence presence)
    {
        if (!IsReady) {
            return;
        }

        presence.Timestamps = _sessionTime;
        _client.SetPresence(presence);

        ErpcMod.Log($"Update presence\n{presence.LogString()}");
    }

    internal void Dispose()
    {
        _client.Dispose();
        IsReady = false;
    }
}