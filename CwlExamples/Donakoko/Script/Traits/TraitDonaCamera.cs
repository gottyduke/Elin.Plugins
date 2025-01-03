using Dona.Common;

namespace Dona.Traits;

internal class TraitDonaCamera : TraitEquipItem
{
    public override void OnEquip(Chara c, bool onSetOwner)
    {
        if (c.id != Constants.CharaId) {
            c.Say("dona_camera_unique");
        }
    }
}