using Cwl.Helper.Unity;
using ReflexCLI.Attributes;

namespace Cwl;

internal partial class DataLoader
{
    [ConsoleCommand("load_sources")]
    internal static void ReloadSources(bool reloadGame = true)
    {
        if (EClass.core.IsGameStarted) {
            if (reloadGame) {
                var game = EClass.game;
                game.Save(silent: true);
                CoroutineHelper.Deferred(() => Game.Load(Game.id, game.isCloud));
            }

            EMono.scene.Init(Scene.Mode.Title);
        }

        var sm = EMono.sources;
        sm.initialized = false;
        sm.Init();
    }
}