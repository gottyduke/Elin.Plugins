using YKF;

namespace Emmersive.API;

internal interface ILayoutProvider
{
    public void OnLayout(YKLayout layout);
    public void OnLayoutConfirm();
}