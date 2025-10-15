using System.Linq;
using Emmersive.API;
using Emmersive.API.Services;
using YKF;

namespace Emmersive.Components;

internal abstract class TabEmmersiveBase : YKLayout<LayerCreationData>
{
    public abstract void OnLayoutConfirm();
}