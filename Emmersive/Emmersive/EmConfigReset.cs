using System;
using System.IO;
using Emmersive.API.Services;
using Emmersive.Components;
using Emmersive.LangMod;
using ReflexCLI.Attributes;

namespace Emmersive;

internal partial class EmConfig
{
    private const EmConfigVersion CurrentVersion = EmConfigVersion.V4;

    private static readonly Debouncer _reloadDebounce = new(0.3f);
    private static string? _savedByMod;

    [ConsoleCommand("reload_cfg")]
    internal static void ReloadWithPopup()
    {
        Reload();
        EmMod.Popup<EmConfig>("em_ui_config_reloaded".lang());
    }

    internal static void Reload()
    {
        EmMod.Instance.Config.Reload();
        var concurrent = Policy.ConcurrentRequests.Value;
        EmScheduler.Semaphore = new(concurrent, concurrent);
    }

    [ConsoleCommand("reset_cfg")]
    internal static void Reset()
    {
        var config = EmMod.Instance.Config;
        var backup = $"{config.ConfigFilePath}.bak";
        try {
            File.Copy(config.ConfigFilePath, backup, true);
        } catch (Exception ex) {
            EmMod.Warn<EmConfig>($"failed to back up config: {ex.Message}");
        }

        File.WriteAllText(config.ConfigFilePath, "");

        config.SaveOnConfigSet = false;
        config.Clear();

        Bind();
        Reload();

        config.Save();
        config.SaveOnConfigSet = true;
        _savedByMod = File.ReadAllText(config.ConfigFilePath);

        EmMod.Popup<EmConfig>("em_ui_config_reset".Loc(CurrentVersion, backup), 10f);
    }

    internal static void InvalidateConfigs()
    {
        var context = ResourceFetch.Context;

        if (!context.Load<EmConfigVersion>("config_version", out var version)) {
            context.SaveUncompressed("config_version", CurrentVersion);
            return;
        }

        if (version >= CurrentVersion) {
            return;
        }

        context.SaveUncompressed("config_version", CurrentVersion);
        Reset();
    }

    internal static void EnableReloadWatcher()
    {
        var config = EmMod.Instance.Config;
        config.SettingChanged += (_, _) => {
            if (config.SaveOnConfigSet) {
                _savedByMod = File.ReadAllText(config.ConfigFilePath);
            }
        };

        FileWatcherHelper.Register(
            "em_config",
            Path.GetDirectoryName(config.ConfigFilePath)!,
            $"{ModInfo.Guid}.cfg",
            args => {
                if (args.ChangeType != WatcherChangeTypes.Changed) {
                    return;
                }

                _reloadDebounce.Trigger(() => {
                    if (File.ReadAllText(config.ConfigFilePath) == _savedByMod) {
                        return;
                    }

                    _savedByMod = null;
                    EmMod.Popup<EmConfig>("em_ui_config_changed".lang());

                    config.SaveOnConfigSet = false;
                    Reload();
                    config.SaveOnConfigSet = true;
                });
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