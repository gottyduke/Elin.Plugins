using Cwl.API.Attributes;

namespace Cwl.Patches.GameSaveLoad;

internal class EnableCheatPatch
{
    [CwlPostLoad]
    [CwlPostSave]
    internal static void SetCheatEnabled()
    {
        EClass.game.config?.cheat = true;
    }

    [CwlPreSave]
    internal static void SetCheatDisabled()
    {
        EClass.game.config?.cheat = false;
    }
}