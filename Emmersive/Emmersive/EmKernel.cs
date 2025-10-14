using Emmersive.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using ReflexCLI.Attributes;
using SceneDirector = Emmersive.API.Services.SceneDirector.SceneDirector;

namespace Emmersive;

[ConsoleCommandClassCustomizer("em")]
public static class EmKernel
{
    public static Kernel? Kernel { get; private set; }

    [ConsoleCommand("rebuild_kernel")]
    public static Kernel RebuildKernel()
    {
        return Kernel = Kernel
            .CreateBuilder()
            .AddScenePlugin()
            .AddChatProviders()
            .Build();
    }

    extension(IKernelBuilder builder)
    {
        private IKernelBuilder AddScenePlugin()
        {
            builder.Services.AddSingleton(new SceneDirector());
            return builder;
        }

        private IKernelBuilder AddChatProviders()
        {
            var apiPool = ApiPoolSelector.Instance;
            builder.Services.AddSingleton<IAIServiceSelector>(apiPool);

            foreach (var provider in apiPool.Providers) {
                provider.Register(builder);
            }

            return builder;
        }
    }
}