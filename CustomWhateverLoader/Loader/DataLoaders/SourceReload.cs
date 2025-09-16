using ReflexCLI.Attributes;

namespace Cwl;

internal partial class DataLoader
{
    [ConsoleCommand("load_sources")]
    internal static void ReloadSources(bool saveGame = false)
    {
        if (EClass.core.IsGameStarted) {
            if (saveGame) {
                EClass.game.Save(silent: true);
            }
            EMono.scene.Init(Scene.Mode.Title);
        }

        var sm = EMono.sources;
        sm.initialized = false;
        sm.Init();
    }
}