using UnityEngine;
using YKF;

namespace Emmersive.Components;

internal class LayerEmmersiveBase<T> : YKLayer<T>
{
    public override Rect Bound => FitWindow();

    private static Rect FitWindow()
    {
        var scaler = ui.canvasScaler.scaleFactor;
        var size = new Vector2(Screen.width / 1.5f, Screen.height / 1.5f) / scaler;
        return new(Vector2.zero, size);
    }
}