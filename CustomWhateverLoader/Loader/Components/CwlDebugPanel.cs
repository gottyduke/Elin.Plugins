using Cwl.API.Attributes;
using Cwl.Helper.Unity;
using ReflexCLI.Attributes;
using UnityEngine;

namespace Cwl.Components;

[ConsoleCommandClassCustomizer("cwl")]
internal class CwlDebugPanel
{
    private static CwlDebugPanel? _panel;
    private ProgressIndicator? _progress;
    private bool _reloadGame;

    [ConsoleCommand("enable_debug")]
    [CwlContextMenu("cwl_ui_debug_btn")]
    internal static void EnableDebugPanel()
    {
        _panel ??= new();
        //_panel.Show();

        ELayerCleanup.Cleanup<LayerDebug>();
        ELayer.ui.AddLayer<LayerDebug>();
    }

    internal void Show()
    {
        Kill();
        var title = $"CWL {ModInfo.BuildVersion} {ModInfo.TargetVersion}";
        _progress = ProgressIndicator
            .CreateProgress(() => new(title), _ => false)
            .OnAfterGUI(DrawDebugPanel);
    }

    internal void Kill()
    {
        _progress?.Kill();
        _progress = null;
    }

    private void DrawDebugPanel(ProgressIndicator progress)
    {
        GUILayout.BeginHorizontal();
        {
            _reloadGame = GUILayout.Toggle(_reloadGame, $"Reload Save: ({_reloadGame})");

            if (GUILayout.Button("Reload Sources")) {
                DataLoader.ReloadSources(_reloadGame);
            }
        }
        GUILayout.EndHorizontal();
    }
}