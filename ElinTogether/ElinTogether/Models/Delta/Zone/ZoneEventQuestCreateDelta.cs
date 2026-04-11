using ElinTogether.Net;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class ZoneEventQuestCreateDelta : ElinDelta
{
    [Key(1)]
    public required RemoteCard RefChara { get; init; }

    protected override void OnApply(ElinNetBase net)
    {
        if (RefChara.Find() is not Chara chara) {
            return;
        }
        
        chara.quest.CreateInstanceZone(chara);
    }
}