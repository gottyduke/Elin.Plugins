using System;
using System.Linq;
using Emmersive.API.Services;
using Emmersive.Contexts;
using Emmersive.Helper;
using EModding.Helper.Runtime.Exceptions;
using YKF;

namespace Emmersive.Components;

internal class TabCharaPrompt : TabEmmersiveBase
{
    private static readonly string[] _popupEmoji = [
        "(∠・ω< )⌒★",
        "(＞ω＜)⌒☆",
        "(◕ω<)⌒✿",
        "(∠▽∠)⌒o",
    ];

    public override void OnLayout()
    {
        var header = Horizontal();
        header.Layout.childForceExpandWidth = true;

        header.Header("em_ui_chara_map");
        header.Button("em_ui_open_folder".lang(), () => ResourceFetch.OpenCustomFolder("Emmersive/Characters"));

        BuildCharacterPromptCards();
    }

    internal static Chara[] GetMapCharas()
    {
        return [
            EClass.pc,
            ..EClass._map.charas
                .Where(c => c.Profile.IsImportant)
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

            card.Toggle("em_ui_use_pop",
                    chara.GetBool("em_pop"),
                    value => {
                        chara.SetBool("em_pop", value);
                        if (value) {
                            WidgetFeed.Instance.SayRaw(chara, _popupEmoji.RandomItem());
                        }
                    })
                .transform
                .SetSiblingIndex(1);
            card.Toggle("em_ui_use_summarize",
                    chara.GetBool("em_sum"),
                    value => chara.SetBool("em_sum", value))
                .transform
                .SetSiblingIndex(2);
            card.Spacer(5)
                .transform
                .SetSiblingIndex(3);
        } catch (Exception ex) {
            if (card != null) {
                DestroyImmediate(card.gameObject);
            }

            DebugThrow.Void(ex);
            // noexcept
        }
    }
}