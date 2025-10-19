using System.Collections.Generic;
using System.Linq;
using Emmersive.Helper;

namespace Emmersive.Contexts;

public class CharaContext(Chara chara) : ContextProviderBase
{
    public override string Name => "character_data";

    protected override IDictionary<string, object> BuildInternal()
    {
        Dictionary<string, object> data = new() {
            ["name"] = chara.NameSimple,
            ["uid"] = chara.uid,
            ["can_talk"] = !chara.Profile.OnTalkCooldown,
            ["title"] = chara.Aka.ToTitleCase(),
            ["level"] = chara.LV,
            ["hp"] = $"{chara.hp}/{chara.MaxHP}",
            ["mana"] = $"{chara.mana.value}/{chara.mana.max}",
            ["stamina"] = $"{chara.stamina.value}/{chara.stamina.max}",
            ["class"] = chara.job.GetName(),
            ["race"] = chara.race.GetText().ToTitleCase(),
            ["age"] = chara.bio.TextAge(chara),
            ["gender"] = Lang._gender(chara.bio.gender),
        };

        var personality = new PersonalityContext(chara).Build();
        if (personality is not null) {
            data["personality"] = personality;
        }

        if (!chara.IsPC) {
            var hostility = chara.hostility;
            data["hostility"] = hostility.ToString();

            if (hostility is not Hostility.Enemy) {
                data["affinity"] = $"{chara.affinity.Name}";
                data["in_party"] = chara.IsPCParty;

                if (chara.faith is not ReligionEyth) {
                    data["faith"] = chara.faith.Name;
                }
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