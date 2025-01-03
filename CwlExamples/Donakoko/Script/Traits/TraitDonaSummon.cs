using System.Linq;
using Cwl.API.Custom;
using Dona.Common;

namespace Dona.Traits;

internal class TraitDonaSummon : TraitItem
{
    public override bool OnUse(Chara c)
    {
        if (!c.IsPC) {
            c.SayNothingHappans();
            return false;
        }
        
        var dona = game.cards.globalCharas.Values.FirstOrDefault(gc => gc.id == Constants.CharaId);
        if (dona is not null) {
            c.Say("dona_duplicate");
            return false;
        }
        
        if (!CustomChara.CreateTaggedChara(Constants.CharaId, out dona, [$"{Constants.CameraId}#Artifact"]) || 
            dona is null) {
            c.Say("dona_failed");
            return false;
        }
        
        _zone.AddCard(dona, c.pos.GetNearestPoint(allowChara: false));
        
        dona.SetFeat(Constants.FeatId);
        dona.MakeAlly();
        dona.PlaySound("identify");
        dona.PlayEffect("teleport");

        owner.ModNum(-1);
        return true;
    }
}