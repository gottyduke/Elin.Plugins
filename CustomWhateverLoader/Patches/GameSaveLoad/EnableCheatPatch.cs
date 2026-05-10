using Cwl.API.Attributes;

namespace Cwl.Patches.GameSaveLoad;

internal class EnableCheatPatch
{
    [CwlPostLoad]
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