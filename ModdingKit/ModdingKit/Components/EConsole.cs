using ReflexCLI.UI;
using UnityEngine;

namespace EModding.Components;

internal class EConsole : EMono
{
    private void Update()
    {
        if (scene.mode is not Scene.Mode.Title) {
            return;
        }

        var pressed = Input.GetKeyDown(EClass.core.config.input.keys.console.key);
        if (ReflexUIManager.IsConsoleOpen() && pressed) {
            ReflexUIManager.StaticClose();
        } else if (pressed) {
            ReflexUIManager.StaticOpen();
        }
    }
}