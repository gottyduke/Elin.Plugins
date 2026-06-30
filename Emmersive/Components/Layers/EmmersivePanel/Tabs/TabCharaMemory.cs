using System.Linq;
using Emmersive.Contexts.Memory;
using Emmersive.Helper;
using UnityEngine.UI;
using YKF;

namespace Emmersive.Components;

internal class TabCharaMemory : TabCharaPrompt
{
    public override void OnLayout()
    {
        var header = Horizontal();
        header.Layout.childForceExpandWidth = true;

        header.Header("em_ui_npc_memory");

        BuildCharaMemoryList();
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
            list.Button($"{memory.Name} ({memory.ShortTerm.Count}+{memory.LongTerm.Count})", () => {
                YK.CreateLayer<LayerMemoryEditor, LayerMemoryCreationData>(new(memory));
            });
        }

        if (memories.Count == 0) {
            this.MakeCard().Text("em_ui_no_memories");
        }
    }
}