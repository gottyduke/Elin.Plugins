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
        if (!File.Exists(path)) {
            return;
        }

        path = path.NormalizePath();

        try {
            _unclosed.Add(Process.Start(path));
            return;
        } catch {
            // noexcept
        }

        // open with notepad
        var proton = !"PROTON_VERSION".EnvVar.IsEmpty() ||
                     !"STEAM_COMPAT_DATA_PATH".EnvVar.IsEmpty();
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

        // open folder
        try {
            path = Path.GetDirectoryName(path)!.NormalizePath();

            if (!Directory.Exists(path)) {
                return;
            }

            if (proton) {
                Process.Start("xdg-open", $"\"{path}\"");
            } else {
                Process.Start("explorer.exe", path);
            }
        } catch {
            // noexcept
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