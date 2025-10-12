using YKF;

namespace Emmersive.Components;

internal class TabEmmersiveTalk : YKLayout<LayerCreationData>
{
    public override void OnLayout()
    {
        var button = Button("test", () => { });
    }
}