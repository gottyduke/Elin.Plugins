using ReflexCLI.Attributes;

namespace Cwl;

internal partial class DataLoader
{
    [ConsoleCommand("load_sources")]
    internal static void ReloadSources()
    {
        if (EClass.core.IsGameStarted) {
            EClass.game.Save(silent: true);
            EMono.scene.Init(Scene.Mode.Title);
        }

        var sm = EMono.sources;
        sm.initialized = false;
        sm.Init();
    }
}