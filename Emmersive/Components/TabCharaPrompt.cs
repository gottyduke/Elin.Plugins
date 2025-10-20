using System;
using System.Linq;
using Cwl.Helper.Exceptions;
using Emmersive.Contexts;
using Emmersive.Helper;
using YKF;

namespace Emmersive.Components;

internal class TabCharaPrompt : TabEmmersiveBase
{
    public override void OnLayout()
    {
        var header = Horizontal();
        header.Layout.childForceExpandWidth = true;

        header.Header("em_ui_chara_map");
        header.Button("em_ui_open_folder".lang(), () => ResourceFetch.OpenCustomFolder("Emmersive/Characters"));

        BuildCharacterPromptCards();

        base.OnLayout();
    }

    internal static Chara[] GetMapCharas()
    {
        return [
            EClass.pc,
            ..EClass._map.charas
                .Where(c => !c.IsPC && !c.IsAnimal && (c.IsUnique || c.IsGlobal))
                .Distinct(UniqueCardComparer.Default)
                .OfType<Chara>()
                .OrderByDescending(c => c.IsPCFaction),
        ];
    }

    internal void BuildCharacterPromptCards()
    {
        foreach (var chara in GetMapCharas()) {
            AddCharaButton(chara);
        }
    }

    internal void AddCharaButton(Chara chara)
    {
        YKLayout? card = null;
        try {
            _ = new BackgroundContext(chara).Build();
            card = BuildPromptCard(chara.Name, $"Emmersive/Characters/{chara.UnifiedId}.txt");
        } catch (Exception ex) {
            if (card != null) {
                DestroyImmediate(card);
            }

            DebugThrow.Void(ex);
            // noexcept
        }
    }
}