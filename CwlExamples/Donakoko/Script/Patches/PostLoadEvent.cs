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

        var dona = game.cards.globalCharas.Values.FirstOrDefault(gc => gc.id == Constants.CharaId);
        if (dona?.IsPCFaction is false) {
            dona.MakeAlly();
        }

        if (dona is not null) {
            return;
        }

        pc.Add(Constants.CameraSummonId);
        Msg.Say("dona_camera_given");
    }
}