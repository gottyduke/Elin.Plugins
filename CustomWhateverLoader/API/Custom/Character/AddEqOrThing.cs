using System;
using Cwl.LangMod;

namespace Cwl.API.Custom;

public partial class CustomChara
{
    private static void AddEqOrThing(Chara chara, string id, string? payload, bool equip = false)
    {
        if (sources.cards.map.TryGetValue(id) is null) {
            CwlMod.WarnWithPopup<CustomChara>("cwl_warn_thing_gen".Loc(id, chara.id));
            return;
        }

        Thing? thing = null;
        try {
            if (equip) {
                if (!Enum.TryParse<Rarity>(payload, true, out var rarity)) {
                    rarity = Rarity.Random;
                }

                thing = chara.EQ_ID(id, r: rarity);
                thing.c_IDTState = 0;

                if (!thing.isDestroyed) {
                    thing.ChangeRarity(rarity);
                    CwlMod.Log<CustomChara>("cwl_log_added_eq".Loc(id, thing.rarity.ToString(), chara.id));

                    if (thing.isEquipped) {
                        return;
                    }
                }
            }

            if (!int.TryParse(payload, out var count)) {
                count = 1;
            }

            thing = ThingGen.Create(id).SetNum(count);
            thing.c_IDTState = 0;
            chara.AddThing(thing);

            CwlMod.Log<CustomChara>("cwl_log_added_thing".Loc(id, thing.Num, chara.id));
        } catch {
            thing?.Destroy();
            CwlMod.WarnWithPopup<CustomChara>("cwl_warn_thing_gen".Loc(id, chara.id));
            // noexcept
        }
    }
}