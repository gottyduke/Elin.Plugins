using System.Linq;
using Cwl.API.Custom;
using Dona.Common;

namespace Dona.Traits;

internal class TraitDonaSummon : TraitItem
{
    // when summon camera is used
    public override bool OnUse(Chara c)
    {
        // only pc can summon dona
        if (!c.IsPC) {
            c.SayNothingHappans();
            return false;
        }

        // if dona already exists
        var dona = game.cards.globalCharas.Values.FirstOrDefault(gc => gc.id == Constants.CharaId);
        if (dona is not null) {
            c.Say("dona_duplicate");

            // consume the camera
            owner.ModNum(-1);
            return false;
        }

        // if the creation of dona failed
        if (!CustomChara.CreateTaggedChara(Constants.CharaId, out dona, [$"{Constants.CameraId}#Artifact"]) ||
            dona is null) {
            c.Say("dona_failed");
            dona?.Destroy();

            // consume the camera
            owner.ModNum(-1);
            return false;
        }

        // spawn dona next to pc
        _zone.AddCard(dona, c.pos.GetNearestPoint(allowChara: false));

        // give dona own feat
        dona.SetFeat(Constants.FeatId);

        // add dona to pc party
        dona.MakeAlly();
        dona.PlaySound("identify");
        dona.PlayEffect("teleport");

        // consume the camera
        owner.ModNum(-1);
        return true;
    }
}