using Microsoft.SemanticKernel;

namespace Emmersive.API.Contexts;

public interface IContextBuilder
{
    public IContextBuilder Add(IContextProvider provider);
    public IContextBuilder Add(params IContextProvider[] providers);
    public KernelArguments Build();
}