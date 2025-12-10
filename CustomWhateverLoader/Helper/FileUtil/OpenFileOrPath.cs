using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Cwl.Helper.String;

namespace Cwl.Helper.FileUtil;

public class OpenFileOrPath
{
    private static readonly List<Process> _unclosed = [];

    public static void Run(string path)
    {
        path = path.NormalizePath();

        try {
            _unclosed.Add(Process.Start(path));
            return;
        } catch {
            // noexcept
        }

        var proton = !"PROTON_VERSION".EnvVar.IsEmptyOrNull ||
                     !"STEAM_COMPAT_DATA_PATH".EnvVar.IsEmptyOrNull;

        if (File.Exists(path)) {
            // open with notepad
            try {
                if (proton) {
                    Process.Start("xdg-open", $"\"{path}\"");
                } else {
                    _unclosed.Add(Process.Start("notepad.exe", path));
                }

                return;
            } catch {
                // noexcept
            }
        }

        if (Directory.Exists(path)) {
            // open folder
            try {
                path = Path.GetDirectoryName(path)!.NormalizePath();

                if (proton) {
                    Process.Start("xdg-open", $"\"{path}\"");
                } else {
                    Process.Start("explorer.exe", path);
                }
            } catch {
                // noexcept
            }
        }
    }

    public static void ForceCloseAllReferences()
    {
        foreach (var process in _unclosed) {
            try {
                process.Kill();
            } catch {
                // noexcept
            }
        }
    }
}