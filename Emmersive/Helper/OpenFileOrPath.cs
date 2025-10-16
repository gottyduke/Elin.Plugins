using System;
using System.Diagnostics;
using System.IO;
using Cwl.Helper.String;
using Cwl.LangMod;

namespace Emmersive.Helper;

public class OpenFileOrPath
{
    public static void Run(string path)
    {
        path = path.NormalizePath();
        try {
            Process.Start(path);
        } catch {
            EmMod.Popup<OpenFileOrPath>("em_ui_failed_shellex".Loc());

            var proton = !"PROTON_VERSION".EnvVar.IsEmpty() ||
                         !"STEAM_COMPAT_DATA_PATH".EnvVar.IsEmpty();
            try {
                if (proton) {
                    Process.Start("xdg-open", $"\"{path}\"");
                } else {
                    Process.Start("notepad.exe", path);
                }
            } catch (Exception ex) {
                EmMod.Popup<OpenFileOrPath>("em_ui_failed_shellex".Loc(path, ex.Message));

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
                // noexcept
            }
            // noexcept
        }
    }
}