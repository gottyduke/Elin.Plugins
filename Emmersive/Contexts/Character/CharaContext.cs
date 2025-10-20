using System;
using System.Collections.Generic;
using System.Linq;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public class CharaContext(Chara chara) : ContextProviderBase
{
    private static readonly Dictionary<Hostility, string> _hostilities = new() {
        [Hostility.Enemy] = "em_hostility_enemy".lang(),
        [Hostility.Neutral] = "em_hostility_neutral".lang(),
        [Hostility.Friend] = "em_hostility_friend".lang(),
        [Hostility.Ally] = "em_hostility_ally".lang(),
    };

    public override string Name => "character_data";

    protected override IDictionary<string, object> BuildInternal()
    {
        var data = new Dictionary<string, object> {
            ["name"] = chara.NameSimple,
            ["uid"] = chara.uid,
            ["can_talk"] = !chara.Profile.OnTalkCooldown,
            ["title"] = chara.Aka.ToTitleCase().IsEmpty(null),
            ["level"] = chara.LV,
            ["hp"] = $"{chara.hp}/{chara.MaxHP}",
            ["mana"] = $"{chara.mana.value}/{chara.mana.max}",
            ["class"] = chara.job.GetName(),
            ["race"] = chara.race.GetText().ToTitleCase(),
            ["age"] = chara.bio.TextAge(chara),
            ["gender"] = Lang._gender(chara.bio.gender),
        };

        if (data["title"] is null) {
            data.Remove("title");
        }

        if (chara.IsPC) {
            data["stamina"] = $"{chara.stamina.value}/{chara.stamina.max}";
        } else {
            var hostility = chara.hostility;
            data["hostility"] = _hostilities[hostility];

            switch (hostility) {
                case Hostility.Enemy:
                case Hostility.Neutral:
                    data.Remove("mana");
                    break;
                case Hostility.Friend or Hostility.Ally:
                    if (chara.IsPCFactionMinion) {
                        data["is_minion"] = true;
                        break;
                    }

                    if (chara.IsPCParty) {
                        data["in_party"] = chara.IsPCParty;
                        data["hobbies"] = chara.GetTextHobby(true);
                        data["fav_food"] = chara.GetFavFood().GetName();

                    }

                    if (chara.faith is not ReligionEyth) {
                        data["faith"] = chara.faith.Name;
                    }

                    data["affinity"] = $"{chara.affinity.Name}";

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (chara.IsAnimal) {
                data["animal"] = true;
            }

            if (!chara.IsHumanSpeak) {
                data["human_language"] = false;
            }
        }

        var conditions = chara.conditions;
        if (conditions.Count > 0) {
            data["conditions"] = string.Join(',', conditions.Select(c => c.Name));
        }

        var background = new BackgroundContext(chara).Build();
        if (background is not null) {
            data["background"] = background;
        }

        return data;
    }
}