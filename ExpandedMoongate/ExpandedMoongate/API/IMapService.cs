using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Exm.Model.Map;

namespace Exm.API;

public interface IMapService : IMapServiceV2;

public interface IMapServiceV2 : IMapServiceV1
{
    public UniTask<MapMeta[]?> GetMapHistoryByUserAsync(string userId);
    public UniTask<MapServiceOverview?> GetMapsOverviewAsync(LangFilter lang, TimePeriod days, string? noTags);
    public UniTask<MapMeta[]?> GetMapMetaByQueryAsync(string query);
    public UniTask<byte[]?> GetMapPreviewAsync(string mapId);
}

public interface IMapServiceV1
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum LangFilter
    {
        All,
        EN,
        CN,
        JP,
    }

    public enum SortType
    {
        Created = 0,
        Rating,
        Visits,
    }

    public enum TimePeriod
    {
        AllTime = -1,
        Today = 1,
        OneWeek = 7,
        ThirtyDays = 30,
        ThreeMonths = 90,
        //SixMonths = 180,
        //OneYear = 360,
    }

    public UniTask<byte[]?> GetMapFileAsync(string mapId);
    public UniTask<MapMeta?> GetMapMetaAsync(string mapId);

    public UniTask<MapMeta[]?> GetTopMapsAsync(SortType sort,
                                               int count,
                                               int page,
                                               LangFilter lang,
                                               TimePeriod days,
                                               string? noTags);

    public UniTask<MapRating?> GetMapRatingByUserAsync(string mapId, string userId);
    public UniTask<bool> PostMapRatingAsync(string mapId, MapRating rating);
}