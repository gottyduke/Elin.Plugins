using Cwl.Helper.Unity;
using Dona.Common;

namespace Dona.Traits;

internal class TraitDonaCamera : TraitEquipItem
{
    public override void OnEquip(Chara c, bool onSetOwner)
    {
        if (c.id != Constants.CharaId) {
            CoroutineHelper.Deferred(
                () => c.Say("dona_camera_unique"),
                () => core.IsGameStarted);
        }
    }
}