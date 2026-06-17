using EModding.Helper;
using ReflexCLI.Attributes;
using UnityEngine;

namespace EModding.Components;

[ConsoleCommandClassCustomizer("mod")]
internal class EDebugInfo : EMono
{
    private EGui? _progress;

    private void Update()
    {
        if (!EClass.core.IsGameStarted) {
            Kill();
            return;
        }

        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.M)) {
            if (_progress is null) {
                Show();
            }
        } else {
            Kill();
        }
    }

    [ConsoleCommand("debug")]
    internal static void ShowDebugPanel()
    {
        ELayerCleanup.Cleanup<LayerDebug>();
        ELayer.ui.AddLayer<LayerDebug>();
    }

    internal void Show()
    {
        Kill();
        _progress = EGui
            .CreatePopup(() => new("Target Info"), _ => false)
            .OnAfterGUI(DrawDebugPanel);
    }

    internal void Kill()
    {
        _progress?.Kill();
        _progress = null;
    }

    private void DrawDebugPanel(EGui p)
    {
        GUILayout.BeginVertical(p.GUIStyle);
        {
            var point = Scene.HitPoint;
            if (point is { detail: { } detail }) {
                foreach (var chara in detail.charas.ToArray()) {
                    GUILayout.Box($"{chara.Name} '{chara.id}'\n" +
                                  $"source: {GetSourceInfo(chara.sourceCard)}\n" +
                                  $"race: {GetSourceInfo(chara.race)}", p.GUIStyle);
                }

                foreach (var thing in detail.things.ToArray()) {
                    GUILayout.Box($"{thing.Name} '{thing.id}'\n" +
                                  $"source: {GetSourceInfo(thing.sourceCard)}", p.GUIStyle);
                }
            }
        }
        GUILayout.EndVertical();

        return;

        string? GetSourceInfo(SourceData.BaseRow row) => ModUtil.FindSourceRowPackage(row)?.title;
    }
}