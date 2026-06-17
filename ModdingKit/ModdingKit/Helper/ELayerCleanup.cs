using System.Linq;

namespace EModding.Helper;

public class ELayerCleanup : ELayer
{
    public static void Cleanup<T>(bool includeInactive = false) where T : ELayer
    {
        foreach (var layer in ui.layers.OfType<T>().ToArray()) {
            if (layer.isActiveAndEnabled || includeInactive) {
                CoroutineHelper.Deferred(layer.Close);
            }
        }
    }
}