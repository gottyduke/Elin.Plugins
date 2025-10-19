using System.IO;
using Cwl.Helper.FileUtil;
using Cwl.LangMod;
using Emmersive.Helper;
using ReflexCLI.Attributes;

namespace Emmersive;

internal partial class EmConfig
{
    private const EmConfigVersion CurrentVersion = EmConfigVersion.V3;

    [ConsoleCommand("reload_cfg")]
    internal static void Reload()
    {
        EmMod.Instance.Config.Reload();
    }

    [ConsoleCommand("reset_cfg")]
    internal static void Reset()
    {
        var config = EmMod.Instance.Config;

        foreach (var entry in config.Values) {
            entry.SetSerializedValue(entry.DefaultValue.ToString());
        }

        config.Save();
        Reload();

        EmMod.Popup<EmConfig>("em_ui_config_reset".Loc(CurrentVersion));
    }

    internal static void InvalidateConfigs()
    {
        var context = ResourceFetch.Context;
        if (context.Load<EmConfigVersion>(out var version, "config_version") &&
            version >= CurrentVersion) {
            return;
        }

        context.SaveUncompressed(CurrentVersion, "config_version");
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
    }
}