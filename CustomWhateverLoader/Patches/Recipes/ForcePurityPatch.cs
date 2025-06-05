using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.Unity;

namespace Cwl.Patches.Recipes;

internal class ForcePurityPatch
{
    [CwlThingOnCreateEvent]
    internal static void OnResetPurity(Thing __instance)
    {
        var row = __instance.source;
        if (row is null) {
            return;
        }

        if (row.tag.Contains("noCopy") && row.elements.Contains(ELEMENT.purity)) {
            CoroutineHelper.Deferred(() => __instance.elements.SetBase(ELEMENT.purity, 10));
        }
    }
}