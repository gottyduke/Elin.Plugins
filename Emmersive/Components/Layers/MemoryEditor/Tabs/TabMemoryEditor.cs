using System.Linq;
using Cysharp.Threading.Tasks;
using Emmersive.API.Plugins;
using Emmersive.Contexts.Memory;
using Emmersive.Helper;
using Emmersive.LangMod;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Components;

internal class TabMemoryEditor : YKLayout<LayerMemoryCreationData>
{
    public override void OnLayout()
    {
        var store = _layer?.Data.Memory;
        if (store is null) {
            return;
        }

        var header = Horizontal();
        header.HeaderCard(store.Name);

        BuildStmSection(store);
        BuildLtmSection(store);

        var actions = Horizontal();
        actions.Layout.childForceExpandWidth = true;

        UIButton? summarize = null;
        summarize = actions.Button("em_ui_summarize_now".lang(), () => Summarize(summarize!).ForgetEx());
        summarize.GetOrCreate<Image>().color = Color.green;
        summarize.SetInteractableWithAlpha(store.ShortTerm.Any(e => !e.Summarized));

        actions.Button("em_ui_clear_memory".lang(), () => UIHelper.ConfirmDanger("em_ui_confirm_clear_memory", () => {
            if (SceneDirector.FindSameMapChara(store.Uid, out var chara)) {
                MemoryManager.Instance.ClearMemory(chara);
                if (LayerMemoryEditor.Instance != null) {
                    EClass.ui.RemoveLayer(LayerMemoryEditor.Instance);
                }
                LayerEmmersivePanel.Instance?.Reopen();
            }
        }, "em_ui_clear_memory")).GetOrCreate<Image>().color = Color.red;

        return;

        async UniTask Summarize(UIButton button)
        {
            button.SetInteractableWithAlpha(false);
            button.mainText.text = "em_ui_summarizing".lang();

            var success = await MemoryManager.Instance.TriggerSummarizeAsync(store);
            await UniTask.Yield();

            if (success) {
                EmMod.Popup<MemoryManager>("em_ui_summarize_done".Loc(MemoryManager.Instance.LastSummarizeAdded));
                LayerMemoryEditor.Instance?.Reopen();
                return;
            }

            if (button != null) {
                button.SetInteractableWithAlpha(true);
                button.mainText.text = "em_ui_summarize_now".lang();
            }

            EmMod.Popup<MemoryManager>(MemoryManager.Instance.LastSummarizeError ?? "em_ui_sum_failed".lang());
        }
    }

    private void BuildStmSection(CharaMemoryStore store)
    {
        var card = this.MakeCard();
        var header = card.Text(StmHeader());
        var stm = store.ShortTerm;
        if (stm.Count == 0) {
            card.Text("em_ui_no_stm");
            return;
        }

        foreach (var entry in stm.ToArray()) {
            var line = card.Horizontal();
            line.Text(entry.Speaker, FontColor.Good);
            line.Spacer(0, 5);
            line.Text(entry.Content, entry.Summarized ? FontColor.Passive : FontColor.DontChange);
            line.FlexWidth();
            line.Button("×", () => {
                store.ShortTerm.Remove(entry);
                DestroyImmediate(line.gameObject);
                header.SetText(StmHeader());
            }).GetOrCreate<Image>().color = Color.red;
        }

        return;

        string StmHeader() => "em_ui_stm_header".Loc($"{store.ShortTerm.Count} / {EmConfig.Memory.MaxStmEntries.Value}");
    }

    private void BuildLtmSection(CharaMemoryStore store)
    {
        var card = this.MakeCard();
        var header = card.Horizontal();
        header.Layout.childForceExpandWidth = true;
        header.Text("em_ui_ltm_header".Loc(store.LongTerm.Count));

        header.Button("em_ui_add_ltm".lang(), () => {
            store.LongTerm.Add(new() { Fact = "" });
            LayerMemoryEditor.Instance?.Reopen();
        });

        if (store.LongTerm.Count == 0) {
            card.Text("em_ui_no_ltm");
            return;
        }

        for (var i = 0; i < store.LongTerm.Count; i++) {
            var fact = store.LongTerm[i];
            var row = card.Horizontal();

            var scoreGroup = row.Horizontal();
            scoreGroup.Button("-", () => {
                fact.Importance = Mathf.Max(1, fact.Importance - 1);
                LayerMemoryEditor.Instance?.Reopen();
            }).LayoutElement().minWidth = 50f;

            var score = scoreGroup.Text($"★{fact.Importance}", FontColor.Good);
            score.LayoutElement().minWidth = 50f;
            score.alignment = TextAnchor.MiddleCenter;

            scoreGroup.Button("+", () => {
                fact.Importance = Mathf.Min(5, fact.Importance + 1);
                LayerMemoryEditor.Instance?.Reopen();
            }).LayoutElement().minWidth = 50f;

            var input = row.PlainTextInput(fact.Fact, 0);

            var idx = i;
            input.field.onValueChanged.AddListener(_ => {
                store.LongTerm[idx].Fact = input.Text;
            });

            row.Button("×", () => {
                store.LongTerm.RemoveAt(idx);
                LayerMemoryEditor.Instance?.Reopen();
            }).GetOrCreate<Image>().color = Color.red;
        }
    }
}