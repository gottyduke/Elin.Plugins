using Cwl.API.Custom;
using Cwl.LangMod;

namespace Cwl.Helper.Extensions;

public static class CharaExt
{
    public static bool IsBoss(this Chara chara, bool hostileOnly = false)
    {
        var bossType = chara.source.tag.Contains("boss") || chara.c_bossType is BossType.Boss or BossType.Evolved;
        var hostile = !hostileOnly || chara.IsHostile();
        return bossType && hostile;
    }

    public static Element? AddElement(this Chara chara, SourceElement.Row element, int power = 1)
    {
        switch (element.group) {
            case nameof(FEAT):
                chara.SetFeat(element.id, power, true);
                break;
            case nameof(ABILITY) or nameof(SPELL):
                chara.GainAbility(element.id, power);
                break;
        }

        CwlMod.Log<CustomElement>("cwl_log_ele_gain".Loc(element.id, element.alias, chara.Name));

        var added = chara.elements.GetOrCreateElement(element.id);
        if (power == 0) {
            return added;
        }

        if (added.source.category == "skill") {
            added.vSourcePotential += added.GetSourcePotential(power);
        }

        added.vSource += added.GetSourceValue(power, chara.LV, SourceValueType.Chara);

        return added;
    }

    public static Element? AddElement(this Chara chara, string alias, int power = 1)
    {
        return EMono.sources.elements.alias.TryGetValue(alias, out var element)
            ? AddElement(chara, element, power)
            : null;
    }

    public static Element? AddElement(this Chara chara, int id, int power = 1)
    {
        return EMono.sources.elements.map.TryGetValue(id, out var element)
            ? AddElement(chara, element, power)
            : null;
    }

    public static string GetUniqueRumor(this Chara chara, bool tone = false)
    {
        var dialog = Lang.GetDialog("unique", chara.id).RandomItem();
        return tone ? chara.ApplyTone(dialog) : dialog;
    }

    public static void DestroyImmediate(this Chara chara)
    {
        var branch = EClass.BranchOrHomeBranch;
        if (branch?.members.Contains(chara) is true) {
            branch.RemoveMemeber(chara);
        }

        var adv = EClass.game.cards.listAdv;
        if (adv.Contains(chara)) {
            adv.Remove(chara);
        }

        chara.Destroy();
    }
}