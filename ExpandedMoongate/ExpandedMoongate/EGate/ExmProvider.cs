using Exm.API;
using Exm.API.Services;

namespace Exm;

public class ExmService
{
    public static IMapService MapService => field ??=
#if DEBUG
        new CloudMapService();
#else
        new CloudMapService();
#endif

    public static MapController MapController => field ??= new(MapService);
}