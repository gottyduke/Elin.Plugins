using Cwl.API.Custom;
using Cwl.API.Drama;
using Cwl.LangMod;

namespace Cwl.Helper.Extensions;

public static class CharaExt
{
    extension(Chara chara)
    {
        public RendererDir RendererDir => (RendererDir)(chara.renderer as CharaRenderer)!.currentDir;

        public bool IsBoss(bool hostileOnly = false)
        {
            var bossType = chara.source.tag.Contains("boss") ||
                           chara.c_bossType is BossType.Boss;
            var hostile = !hostileOnly || chara.IsHostile();
            return bossType && hostile;
        }

        public Element? AddElement(SourceElement.Row element, int power = 1)
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

            added.vSource += (int)added.GetSourceValue(power, chara.LV, SourceValueType.Chara);

            return added;
        }

        public Element? AddElement(string alias, int power = 1)
        {
            return EMono.sources.elements.alias.TryGetValue(alias, out var element)
                ? AddElement(chara, element, power)
                : null;
        }

        public Element? AddElement(int id, int power = 1)
        {
            return EMono.sources.elements.map.TryGetValue(id, out var element)
                ? AddElement(chara, element, power)
                : null;
        }

        public string GetUniqueRumor()
        {
            if (chara.interest <= 0) {
                return chara.GetDialogText("rumor", "bored");
            }

            if (EClass.rnd(20) == 0 || EClass.debug.showFav) {
                var list = chara.ListHobbies();
                var hobby = list.TryGet(0, true);
                if (EClass.rnd(2) == 0 || hobby is null) {
                    GameLang.refDrama1 = chara.GetFavCat().GetName().ToLower();
                    GameLang.refDrama2 = chara.GetFavFood().GetName();
                    chara.knowFav = true;
                    return chara.GetDialogText("general", "talk_fav");
                }

                GameLang.refDrama1 = hobby.Name.ToLower();
                return chara.GetDialogText("general", "talk_hobby");
            }

            if (chara.HasRumorText("unique")) {
                return chara.GetDialogText("unique", chara.id);
            }

            if (EClass.rnd(2) == 0 && !chara.trait.IDRumor.IsEmpty()) {
                return chara.GetDialogText("rumor", chara.trait.IDRumor);
            }

            if (EClass.rnd(2) == 0 && chara.HasRumorText("zone", EClass._zone.id)) {
                return chara.GetDialogText("zone", EClass._zone.id);
            }

            if (EClass.rnd(2) == 0) {
                return chara.GetDialogText("rumor", "interest_" + chara.bio.idInterest.ToEnum<Interest>());
            }

            if (EClass.rnd(2) == 0) {
                return chara.GetTalkText("rumor");
            }

            return chara.GetDialogText("rumor", EClass.rnd(4) == 0 ? "hint" : "default");
        }

        public bool HasRumorText(string idSheet, string? idTopic = null)
        {
            idTopic = idTopic.IsEmpty(chara.id);
            var rumors = Lang.GetDialog(idSheet, idTopic);
            return rumors.Length > 1 || rumors.TryGet(0, true) != idTopic;
        }

        public string GetDialogText(string idSheet, string idTopic)
        {
            var dm = DramaExpansion.Cookie?.Dm;
            if (!idTopic.IsEmpty() && (dm?.customTalkTopics.TryGetValue(idTopic, out var text) ?? false)) {
                return text;
            }

            var dialog = Lang.GetDialog(idSheet, idTopic).RandomItem();
            return dm?.enableTone ?? false ? chara.ApplyTone(dialog) : dialog;
        }

        public void DestroyImmediate()
        {
            if (chara.homeBranch is { } branch) {
                branch.BanishMember(chara, true);
            }

            chara.SetFaction(EClass.Wilds);
            EClass.game.cards.listAdv.Remove(chara);

            chara.Destroy();
        }
    }
}