using Cwl.API.Attributes;

namespace Cwl.Patches.GameSaveLoad;

internal class EnableCheatPatch
{
    [CwlPostLoad]
    internal static void SetCheatEnabled()
    {
        EClass.game?.config?.cheat = true;
    }
}