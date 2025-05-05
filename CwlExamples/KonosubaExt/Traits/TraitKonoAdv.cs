using KonoExt.Common;

namespace KonoExt.Traits;

internal class TraitKonoAdv : TraitAdventurerBacker
{
    internal static void TransformKono(ref string traitName, Card traitOwner)
    {
        if (traitName != nameof(TraitAdventurerBacker)) {
            return;
        }

        if (Constants.KonosubaAdvs.TryGetValue(traitOwner.id, out var konoTrait)) {
            traitName = konoTrait;
        }
    }
}