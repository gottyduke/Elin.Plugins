using System.Linq;

namespace Cwl.Helper.Unity;

public class ELayerCleanup
{
    public static void Cleanup<T>(bool includeInactive = false) where T : ELayer
    {
        foreach (var layer in ELayer.ui.layers.OfType<T>()) {
            if (layer.isActiveAndEnabled || includeInactive) {
                CoroutineHelper.Deferred(layer.Close);
            }
        }
    }
}