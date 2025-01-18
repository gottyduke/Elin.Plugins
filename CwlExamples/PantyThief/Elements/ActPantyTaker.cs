using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.String;

namespace Panty.Elements;

internal class ActPantyTaker : AI_TargetChara
{
    private static readonly int _takerInt = (int)"PantyTaker".Fnv1A();

    public override bool IsValidTC(Card card)
    {
        return card is Chara { IsPC: false, hostility: >= Hostility.Neutral };
    }

    public override IEnumerable<Status> Run()
    {
        if (!IsValidTC(TC)) {
            yield return Cancel();
            yield break;
        }

        if (TC is Chara chara && PantyTaken(chara)) {
            owner.Say("act_panty_taken", chara);
            yield return Cancel();
            yield break;
        }

        var panty = TC.things
            .FindAll(t => t.id == "panty")
            .OrderByDescending(t => t.GetValue())
            .FirstOrDefault();
        if (panty is null) {
            panty = ThingGen.Create("panty");
            panty.ChangeRarity(Rarity.Artifact);
            panty.ChangeMaterial(MATERIAL.GetRandomMaterial(TC.LV));
            panty.SetLv(TC.LV);
            panty.SetEncLv(rnd(10) + 3);
        }

        panty.c_idRefCard = TC.id;

        var targetParent = panty.parent;
        var root = TC.GetRootCard();

        yield return Do(new Progress_Custom {
            canProgress = () => panty.parent == targetParent && TC.ExistsOnMap,
            onProgressBegin = () => owner.PlaySound("PantyTaker/steal"),
            onProgress = _ => {
                owner.LookAt(root);
                owner.PlaySound("steal");
                root.renderer.PlayAnime(AnimeID.Shiver);
            },
            onProgressComplete = () => {
                TC.SetInt(_takerInt, 1);
                TC.Say("act_panty_victim", TC);

                if (TC.bio.gender == 1) {
                    TC.PlaySound("PantyTaker/nani");
                }

                owner.Say("act_panty_taker_success");
                owner.Pick(panty);

                owner.elements.ModExp(281, 50);
                owner.stamina.Mod(-1);
            },
        }.SetDuration(20));
    }
    
    private static bool PantyTaken(Chara chara)
    {
        return chara.GetInt(_takerInt, 0) != 0;
    }
}