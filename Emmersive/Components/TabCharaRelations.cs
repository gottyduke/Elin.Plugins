using System.Linq;
using Emmersive.Contexts;
using Emmersive.Helper;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Components;

internal class TabCharaRelations : TabCharaPrompt
{
    private static Vector2 _browsedPosition = new(0f, 1f);

    private bool _repaint;

    private void Update()
    {
        if (_repaint) {
            _browsedPosition = GetComponentInParent<UIScrollView>().normalizedPosition;
        }
    }

    public override void OnLayout()
    {
        var header = Horizontal();
        header.Layout.childForceExpandWidth = true;

        header.Header("em_ui_chara_relations");
        header.Button("em_ui_open_folder".lang(), () => ResourceFetch.OpenCustomFolder("Emmersive/Relations"));

        var charas = GetMapCharas()
            .ToDictionary(c => c, _ => false);

        BuildRelationGenerator();

        foreach (var relations in RelationContext.Lookup) {
            foreach (var relation in relations) {
                if (relation is null) {
                    continue;
                }

                var names = relation.Rows.Join(r => r.GetText());
                BuildPromptCard(names, $"Emmersive/Relations/{relation.Provider.Name}");
            }
        }

        GetComponentInParent<UIScrollView>().normalizedPosition = _browsedPosition;
        _repaint = true;

        return;

        void BuildRelationGenerator()
        {
            var generator = this.MakeCard();
            generator.TextFlavor("em_ui_edit_relations");

            generator.Button("em_ui_generate_relation".lang(), () => {
                var ids = charas
                    .Where(kv => kv.Value)
                    .Select(kv => kv.Key.id)
                    .ToArray();

                if (ids.Length < 2) {
                    EmMod.Popup<RelationContext>("em_ui_relation_constraint".lang());
                    return;
                }

                var relationKey = RelationContext.GetRelationKey(ids);
                ResourceFetch.OpenOrCreateCustomResource($"Emmersive/Relations/{relationKey}.txt");
            });

            var list = generator.Grid()
                .WithConstraintCount(2);
            list.Fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            list.Layout.cellSize = FitCell(2);

            foreach (var chara in GetMapCharas()) {
                list.Toggle(chara.Name, charas[chara], value => charas[chara] = value);
            }
        }
    }

    private static Vector2 FitCell(int cellSize)
    {
        var scaler = EMono.ui.canvasScaler.scaleFactor;
        return new Vector2(Screen.width / 1.7f / cellSize, 45f) / scaler;
    }
}