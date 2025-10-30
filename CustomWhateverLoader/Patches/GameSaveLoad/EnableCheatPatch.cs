using Cwl.API.Attributes;

namespace Cwl.Patches.GameSaveLoad;

internal class EnableCheatPatch
{
    [CwlSceneInitEvent(Scene.Mode.StartGame)]
    internal static void SetCheatEnabled()
    {
        EClass.game?.config?.cheat = true;
    }
}