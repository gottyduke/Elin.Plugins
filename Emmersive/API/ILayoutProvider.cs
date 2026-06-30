using YKF;

namespace Emmersive.API;

public interface ILayoutProvider
{
    public void OnLayout(YKLayout layout);
    public void OnLayoutConfirm();
}