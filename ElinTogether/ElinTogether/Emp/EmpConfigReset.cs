using System.IO;
using ElinTogether.LangMod;
using ElinTogether.Net;
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
        File.WriteAllText(config.ConfigFilePath, "");

        config.SaveOnConfigSet = false;
        config.Clear();

        Bind();
        Reload();

        config.Save();
        config.SaveOnConfigSet = true;

        EmpPop.PopupInternal("emp_ui_config_reset".Loc(CurrentVersion));
    }

    internal static void InvalidateConfigs()
    {
        var context = GameIOContext.GetPersistentModContext("ElinMP")!;
        if (context.Load<EmpConfigVersion>("config_version", out var version) &&
            version >= CurrentVersion) {
            return;
        }

        context.SaveUncompressed("config_version", CurrentVersion);
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

                if (NetSession.Instance.Connection is ElinNetHost host) {
                    host.UpdateRemoteSessionRules();
                }
            });
    }

    private enum EmpConfigVersion
    {
        V1,
    }
}