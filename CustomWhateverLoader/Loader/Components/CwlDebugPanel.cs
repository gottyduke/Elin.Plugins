using Cwl.API.Attributes;
using Cwl.Helper.Unity;
using ReflexCLI.Attributes;
using UnityEngine;

namespace Cwl.Components;

[ConsoleCommandClassCustomizer("cwl")]
internal class CwlDebugPanel : EMono
{
    private ProgressIndicator? _progress;

    private void Update()
    {
        if (!EClass.core.IsGameStarted) {
            Kill();
            return;
        }

        var triggered = Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.M);
        if (triggered) {
            if (_progress is null) {
                Show();
            }
        } else {
            Kill();
        }
    }

    [ConsoleCommand("enable_debug")]
    [CwlContextMenu("cwl_ui_debug_btn")]
    internal static void EnableDebugPanel()
    {
        ELayerCleanup.Cleanup<LayerDebug>();
        ELayer.ui.AddLayer<LayerDebug>();
    }

    internal void Show()
    {
        Kill();
        _progress = ProgressIndicator
            .CreateProgress(() => new("CWL Debug"), _ => false)
            .OnAfterGUI(DrawDebugPanel);
    }

    internal void Kill()
    {
        _progress?.Kill();
        _progress = null;
    }

    private void DrawDebugPanel(ProgressIndicator p)
    {
        GUILayout.BeginVertical(p.GUIStyle);
        {
            var point = Scene.HitPoint;
            if (point is { detail: { } detail }) {
                foreach (var chara in detail.charas.ToArray()) {
                    GUILayout.Box($"{chara.Name} '{chara.id}'\n" +
                                  $"row: {GetSourceInfo(chara.sourceCard)}\n" +
                                  $"race: {GetSourceInfo(chara.race)}", p.GUIStyle);
                }

                foreach (var thing in detail.things.ToArray()) {
                    GUILayout.Box($"{thing.Name} '{thing.id}'\n" +
                                  $"row: {GetSourceInfo(thing.sourceCard)}", p.GUIStyle);
                }
            }
        }
        GUILayout.EndVertical();

        return;

        string? GetSourceInfo(SourceData.BaseRow row) => ModUtil.FindSourceRowPackage(row)?.title;
    }
}