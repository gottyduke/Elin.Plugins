using System.IO;
using Cwl.LangMod;
using Exm.Helper;
using ReflexCLI.Attributes;

namespace Exm;

internal partial class ExmConfig
{
    private const EgConfigVersion CurrentVersion = EgConfigVersion.V1;

    [ConsoleCommand("reload_cfg")]
    internal static void Reload()
    {
        ExmMod.Instance.Config.Reload();
    }

    [ConsoleCommand("reset_cfg")]
    internal static void Reset()
    {
        var config = ExmMod.Instance.Config;

        foreach (var entry in config.Values) {
            entry.SetSerializedValue(entry.DefaultValue.ToString());
        }

        config.Save();
        Reload();

        ExmMod.Popup<ExmConfig>("eg_ui_config_reset".Loc(CurrentVersion));
    }

    internal static void InvalidateConfigs()
    {
        var context = ResourceFetch.Context;
        if (context.Load<EgConfigVersion>(out var version, "config_version") &&
            version >= CurrentVersion) {
            return;
        }

        context.SaveUncompressed(CurrentVersion, "config_version");
        Reset();
    }

    internal static void EnableReloadWatcher()
    {
        var config = ExmMod.Instance.Config;

        FileWatcherHelper.Register(
            "eg_config",
            Path.GetDirectoryName(config.ConfigFilePath)!,
            $"{ModInfo.Guid}.cfg",
            args => {
                if (args.ChangeType != WatcherChangeTypes.Changed) {
                    return;
                }

                ExmMod.Popup<ExmConfig>("eg_ui_config_changed".lang());

                config.SaveOnConfigSet = false;
                config.Reload();
                config.SaveOnConfigSet = true;
            });
    }

    private enum EgConfigVersion
    {
        V1, // 0.1.0 beta
    }
}