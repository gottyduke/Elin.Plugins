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

        actions.Button("em_ui_summarize_now".lang(), () => {
            EmMod.Popup<MemoryManager>("em_ui_summarizing".lang());
            Summarize().ForgetEx();
        }).GetOrCreate<Image>().color = Color.green;

        actions.Button("em_ui_clear_memory".lang(), () => {
            if (SceneDirector.FindSameMapChara(store.Uid, out var chara)) {
                MemoryManager.Instance.ClearMemory(chara);
                LayerMemoryEditor.Instance?.Reopen();
            }
        }).GetOrCreate<Image>().color = Color.red;

        return;

        async UniTask Summarize()
        {
            var success = await MemoryManager.Instance.TriggerSummarizeAsync(store);
            await UniTask.Yield();
            if (success) {
                EmMod.Popup<MemoryManager>("em_ui_summarize_done".lang());
                LayerMemoryEditor.Instance?.Reopen();
            } else {
                EmMod.Popup<MemoryManager>("failed to summarize");
            }
        }
    }

    private void BuildStmSection(CharaMemoryStore store)
    {
        var card = this.MakeCard();
        var header = card.Text("em_ui_stm_header");

        var stm = store.GetRecentStm();
        if (stm.Count == 0) {
            card.Text("em_ui_no_stm");
            return;
        }

        foreach (var entry in stm) {
            var line = card.Horizontal();
            line.Text(entry.Speaker, FontColor.Good);
            line.Spacer(0, 5);
            line.Text(entry.Content);
            line.FlexWidth();
            line.Button("x", () => {
                stm.Remove(entry);
                store.ShortTerm.Remove(entry);
                DestroyImmediate(line.gameObject);
                RefreshHeader();
            }).GetOrCreate<Image>().color = Color.red;
        }

        RefreshHeader();

        return;

        void RefreshHeader()
        {
            header.SetText("em_ui_stm_header".Loc($"{store.ShortTerm.Count} / {EmConfig.Memory.MaxStmEntries.Value}"));
        }
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

            var input = row.InputText(fact.Fact);
            input.type = UIInputText.Type.Name;
            input.field.characterLimit = 150;
            input.field.contentType = InputField.ContentType.Standard;
            input.field.inputType = InputField.InputType.Standard;
            input.field.characterValidation = InputField.CharacterValidation.None;
            input.Text = fact.Fact;
            input.LayoutElement().preferredWidth = 1919_810f;

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