using System.Linq;
using Cwl.API.Processors;
using Dona.Common;

namespace Dona.Patches;

internal class PostLoadEvent : EClass
{
    internal static void AddItemIfMissing(GameIOProcessor.GameIOContext context)
    {
        if (!core.IsGameStarted) {
            return;
        }

        // if dona already exists
        var dona = game.cards.globalCharas.Values.FirstOrDefault(gc => gc.id == Constants.CharaId);
        if (dona is not null) {
            if (!dona.IsPCFaction) {
                // but not in player party, then add to party
                dona.MakeAlly();
            }
            return;
        }

        // if player already has the summon camera
        if (pc.things.Find(Constants.CameraSummonId) is not null) {
            return;
        }

        // give player summon camera
        pc.Add(Constants.CameraSummonId);
        Msg.Say("dona_camera_given");
    }
}