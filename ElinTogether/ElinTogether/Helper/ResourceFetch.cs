using System.IO;
using Cwl.API.Processors;
using UnityEngine;

namespace ElinTogether.Helper;

internal class ResourceFetch
{
    internal static readonly GameIOProcessor.GameIOContext Context = GameIOProcessor.GetPersistentModContext("ElinMP")!;

    internal static string TempFolder { get; private set; } = Path.Combine(Application.persistentDataPath, "ElinMP/Temp");

    internal static void SetTempFolder(string path)
    {
        TempFolder = path;
        EmpPop.PopupInternal("Set temp folder to new path\n{Path}", path);
    }

    internal static string GetEmpSavePath()
    {
        var tempSave = Path.Combine(CorePath.RootSave, "world_emp");
        return tempSave;
    }

    internal static void InvalidateTemp()
    {
        if (Directory.Exists(TempFolder)) {
            try {
                Directory.Delete(TempFolder, true);
            } catch {
                // noexcept
            }
        }

        var tempSave = GetEmpSavePath();
        if (Directory.Exists(tempSave)) {
            try {
                Directory.Delete(tempSave, true);
            } catch {
                // noexcept
            }
        }

        EmpLog.Verbose("Cleared temp folder");
    }
}