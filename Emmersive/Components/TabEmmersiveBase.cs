using System.Linq;
using Emmersive.API;
using Emmersive.API.Services;
using YKF;

namespace Emmersive.Components;

internal abstract class TabEmmersiveBase : YKLayout<LayerCreationData>
{
    public virtual void OnLayoutConfirm()
    {
        var layouts = ApiPoolSelector.Instance.Providers
            .OfType<ILayoutProvider>();
        foreach (var provider in layouts) {
            provider.OnLayoutConfirm();
        }
    }
}