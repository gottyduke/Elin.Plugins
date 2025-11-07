using System;
using System.Collections.Generic;
using System.Linq;

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
        var hostility = chara.hostility;
        var data = new Dictionary<string, object> {
            ["uid"] = chara.uid,
            //["can_talk"] = !chara.Profile.OnTalkCooldown,
            ["title"] = chara.Aka.ToTitleCase().IsEmpty(null),
            ["hp"] = $"{chara.hp}/{chara.MaxHP}",
            ["class"] = $"LV.{chara.LV} {chara.race.GetText().ToTitleCase()}",
        };

        if (chara.job.id != "none") {
            data["class"] += $" {chara.job.GetName().ToTitleCase()}";
        }

        if (data["title"] is null) {
            data.Remove("title");
        }

        if (chara.IsPC) {
            data["stamina"] = $"{chara.stamina.value}/{chara.stamina.max}";
        } else {
            switch (hostility) {
                case Hostility.Enemy:
                case Hostility.Neutral:
                    break;
                case Hostility.Friend or Hostility.Ally:
                    if (chara.IsPCFactionMinion) {
                        data["minion"] = true;
                        break;
                    }

                    data["bio"] = $"{Lang._gender(chara.bio.gender)} {chara.bio.TextAge(chara)}";

                    if (chara.IsPCParty) {
                        data["pc_party"] = chara.IsPCParty;
                        data["fav"] = $"{chara.GetFavFood().GetName()}, {chara.GetTextHobby(true)}";
                        data["mana"] = $"{chara.mana.value}/{chara.mana.max}";
                    } else {
                        if (chara.IsAdventurer) {
                            data["adventurer"] = true;
                        }
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

            data["class"] = $"{_hostilities[hostility]} {data["class"]}";
        }

        var conditions = chara.conditions;
        if (conditions.Count > 0) {
            data["condition"] = string.Join(',', conditions.Select(c => c.Name));
        }

        var background = new BackgroundContext(chara).Build();
        if (background is not null) {
            data["persona"] = background;
        }

        return data;
    }
}