using Exm.API;
using Exm.API.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Exm;

public class ExmService
{
    public static ServiceProvider Provider { get; private set; } = null!;

    public static void Build()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMapService, CloudMapService>();
        services.AddScoped<MapController>();

        Provider = services.BuildServiceProvider();
    }
}