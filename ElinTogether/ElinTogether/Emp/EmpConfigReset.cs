using System.IO;
using Cwl.API.Processors;
using Cwl.Helper.FileUtil;
using Cwl.LangMod;
using ReflexCLI.Attributes;

namespace ElinTogether;

internal partial class EmpConfig
{
    private const EmpConfigVersion CurrentVersion = EmpConfigVersion.V1;

    [ConsoleCommand("reload_cfg")]
    internal static void Reload()
    {
        EmpMod.Instance.Config.Reload();
    }

    [ConsoleCommand("reset_cfg")]
    internal static void Reset()
    {
        var config = EmpMod.Instance.Config;

        foreach (var entry in config.Values) {
            entry.SetSerializedValue(entry.DefaultValue.ToString());
        }

        config.Save();
        Reload();

        EmpPop.PopupInternal("emp_ui_config_reset".Loc(CurrentVersion));
    }

    internal static void InvalidateConfigs()
    {
        var context = GameIOProcessor.GetPersistentModContext("ElinMP")!;
        if (context.Load<EmpConfigVersion>(out var version, "config_version") &&
            version >= CurrentVersion) {
            return;
        }

        context.SaveUncompressed(CurrentVersion, "config_version");
        Reset();
    }

    internal static void EnableReloadWatcher()
    {
        var config = EmpMod.Instance.Config;

        FileWatcherHelper.Register(
            "emp_config",
            Path.GetDirectoryName(config.ConfigFilePath)!,
            $"{ModInfo.Guid}.cfg",
            args => {
                if (args.ChangeType != WatcherChangeTypes.Changed) {
                    return;
                }

                EmpPop.PopupInternal("emp_ui_config_changed".lang());

                config.SaveOnConfigSet = false;
                config.Reload();
                config.SaveOnConfigSet = true;
            });
    }

    private enum EmpConfigVersion
    {
        V1,
    }
}