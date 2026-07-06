using System.Linq;
using Emmersive.API.Plugins;
using Emmersive.Contexts.Memory;
using Emmersive.Helper;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Components;

internal class TabCharaMemory : TabCharaPrompt
{
    private UIButton? _memoryToggle;

    public override void OnLayout()
    {
        var header = Horizontal();
        header.Layout.childForceExpandWidth = true;

        _memoryToggle = header.Toggle(
            GetCurrentMemoryState(),
            EmConfig.Memory.Enabled.Value,
            value => {
                EmConfig.Memory.Enabled.Value = value;
                _memoryToggle?.mainText.text = GetCurrentMemoryState();
            });
        _memoryToggle.GetOrCreate<LayoutElement>().minWidth = 80f;

        BuildPromptCard("em_ui_memory_prompt", "Emmersive/MemoryPrompt.txt");

        BuildCharaMemoryList();

        return;

        string GetCurrentMemoryState()
        {
            var isOn = EmConfig.Memory.Enabled.Value;
            return "em_ui_npc_memory".lang() + $": {(isOn ? "on" : "off").lang()}";
        }
    }

    private void BuildCharaMemoryList()
    {
        var generator = this.MakeCard();
        generator.TextFlavor("em_ui_edit_memory");

        var list = generator.Grid()
            .WithConstraintCount(2);
        list.Fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        list.Layout.cellSize = FitCell(2);

        var memories = MemoryManager.Instance.AllStores
            .Where(s => !s.IsEmpty && s.Uid != EClass.pc.uid)
            .OrderByDescending(s => s.LongTerm.Count + s.ShortTerm.Count)
            .ToList();

        foreach (var memory in memories) {
            if (!SceneDirector.FindSameMapChara(memory.Uid, out var chara)) {
                continue;
            }
            if (!chara.Profile.IsImportant && !chara.IsPC) {
                continue;
            }
            list.Button($"{memory.Name} ({memory.ShortTerm.Count}+{memory.LongTerm.Count})", () => {
                YK.CreateLayer<LayerMemoryEditor, LayerMemoryCreationData>(new(memory));
            });
        }

        if (memories.Count == 0) {
            this.MakeCard().Text("em_ui_no_memories");
        }
    }
}