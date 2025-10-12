using System.Collections.Generic;
using Cwl.Helper.FileUtil;
using Emmersive.API.Plugins.SceneDirector;
using Emmersive.API.Services;
using Emmersive.ChatProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using ReflexCLI.Attributes;

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

#if DEBUG
            var (_, keys) = PackageIterator
                .GetJsonFromPackage<Dictionary<string, string[]>>("Emmersive/DebugKeys.json", ModInfo.Guid);

            foreach (var key in keys!["Em_GoogleGeminiAPI_Dummy"]) {
                //apiPool.Register(builder, new GoogleChatProvider(key));
            }

            foreach (var key in keys!["Em_DeepSeekAPI_Dummy"]) {
                //apiPool.Register(builder, new OpenAIProvider(key, "https://api.deepseek.com/v1", "DeepSeek"));
            }

            foreach (var key in keys!["Em_OpenAIAPI_Dummy"]) {
                apiPool.Register(builder, new OpenAIProvider(key));
            }
#endif

            return builder;
        }
    }
}