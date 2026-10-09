using System;
using System.Net.Http;
using Emmersive.API.Services;
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
            .AddExtensionHandler()
            .AddChatProviders()
            .Build();
    }

    extension(IKernelBuilder builder)
    {
        private IKernelBuilder AddExtensionHandler()
        {
            builder.Services.AddSingleton(new HttpClient(ExtensionRequestHandler.Instance, false));
            return builder;
        }

        private IKernelBuilder AddChatProviders()
        {
            var apiPool = ApiPoolSelector.Instance;
            builder.Services.AddSingleton<IAIServiceSelector>(apiPool);

            foreach (var provider in apiPool.Providers) {
                try {
                    provider.Register(builder);
                } catch (Exception ex) {
                    EmMod.Warn<ApiPoolSelector>($"failed to register {provider.Id}\n{ex}");
                    if (provider.IsAvailable) {
                        provider.MarkUnavailable("em_ui_err_register".lang());
                    }
                }
            }

            return builder;
        }
    }
}