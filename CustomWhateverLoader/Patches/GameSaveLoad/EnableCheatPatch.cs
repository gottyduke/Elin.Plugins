using Cwl.API.Attributes;
using Cwl.API.Processors;

namespace Cwl.Patches.GameSaveLoad;

internal class EnableCheatPatch
{
    [CwlPostLoad]
    internal static void SetCheatEnabled(GameIOProcessor.GameIOContext context)
    {
        EClass.game?.config?.cheat = true;
    }
}