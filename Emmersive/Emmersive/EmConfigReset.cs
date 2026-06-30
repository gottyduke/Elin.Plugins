using System.IO;
using Emmersive.API.Services;
using Emmersive.Components;
using Emmersive.LangMod;
using ReflexCLI.Attributes;

namespace Emmersive;

internal partial class EmConfig
{
    private const EmConfigVersion CurrentVersion = EmConfigVersion.V4;

    [ConsoleCommand("reload_cfg")]
    internal static void Reload()
    {
        EmMod.Instance.Config.Reload();
        EmScheduler.Semaphore = new(1, Policy.ConcurrentRequests.Value);
    }

    [ConsoleCommand("reset_cfg")]
    internal static void Reset()
    {
        var config = EmMod.Instance.Config;
        File.WriteAllText(config.ConfigFilePath, "");

        config.SaveOnConfigSet = false;
        config.Clear();

        Bind();
        Reload();

        config.Save();
        config.SaveOnConfigSet = true;

        EmMod.Popup<EmConfig>("em_ui_config_reset".Loc(CurrentVersion));
    }

    internal static void InvalidateConfigs()
    {
        var context = ResourceFetch.Context;
        if (context.Load<EmConfigVersion>("config_version", out var version) &&
            version >= CurrentVersion) {
            return;
        }

        context.SaveUncompressed("config_version", CurrentVersion);
        Reset();
    }

    internal static void EnableReloadWatcher()
    {
        var config = EmMod.Instance.Config;

        FileWatcherHelper.Register(
            "em_config",
            Path.GetDirectoryName(config.ConfigFilePath)!,
            $"{ModInfo.Guid}.cfg",
            args => {
                if (args.ChangeType != WatcherChangeTypes.Changed) {
                    return;
                }

                EmMod.Popup<EmConfig>("em_ui_config_changed".lang());

                config.SaveOnConfigSet = false;
                config.Reload();
                config.SaveOnConfigSet = true;
            });
    }

    private enum EmConfigVersion
    {
        V1, // 0.9.3 beta
        V2, // 0.9.4 beta
        V3, // 0.9.5 beta
        V4, // 0.10.0 beta
    }
}