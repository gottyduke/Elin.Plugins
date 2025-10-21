namespace Emmersive.Test;

[TestClass]
public sealed class KernelTest
{
    [TestMethod]
    public void BuildKernel()
    {
        var kernel = EmKernel.RebuildKernel();
    }
}