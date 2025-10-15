using System;
using System.Diagnostics;
using Cwl.Helper.String;
using Cwl.LangMod;

namespace Cwl.Helper.FileUtil;

public class OpenFileOrPath
{
    public static void Run(string path)
    {
        path = path.NormalizePath();
        try {
            Process.Start(path);
        } catch {
            CwlMod.Popup<OpenFileOrPath>("cwl_ui_failed_shellex".Loc());

            var proton = !"PROTON_VERSION".EnvVar.IsEmpty() ||
                         !"STEAM_COMPAT_DATA_PATH".EnvVar.IsEmpty();

            try {
                if (proton) {
                    Process.Start("xdg-open", $"\"{path}\"");
                } else {
                    Process.Start("notepad.exe", path);
                }
            } catch (Exception ex) {
                CwlMod.Popup<OpenFileOrPath>("cwl_ui_failed_shellex".Loc(path, ex.Message));

                try {
                    if (proton) {
                        Process.Start("xdg-open", $"\"{path}\"");
                    } else {
                        Process.Start("notepad.exe", path);
                    }
                } catch {
                    // noexcept
                }
                // noexcept
            }
            // noexcept
        }
    }
}