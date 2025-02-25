namespace Cwl.Helper.Runtime.Stubs;

public abstract class MethodStub
{
    public virtual void OnEnable()
    {
    }

    public virtual void OnDisable()
    {
    }

    public abstract void Begin();
    public abstract void End();
}