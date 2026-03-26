using Cysharp.Threading.Tasks;
using Exm.Model.Map;

namespace Exm.API;

public interface IMapService : IMapServiceV1
{
}

public interface IMapServiceV2
{
    public UniTask<MapMeta?> GetMapMetaHistoryAsync(string mapId);
    public UniTask<byte[]?> GetMapPreviewAsync(string mapId);
    public UniTask<bool> UploadMapPreviewAsync(string mapId, byte[] bytes);
}

public interface IMapServiceV1
{
    public enum SortType
    {
        Created,
        Rating,
        Visits,
    }

    public UniTask<byte[]?> GetMapFileAsync(string mapId);
    public UniTask<MapMeta?> GetMapMetaAsync(string mapId);
    public UniTask<MapMeta[]> GetTopMapsAsync(SortType sort, int count, int page, string? lang, string? noTags);
    public UniTask<MapRating?> GetMapRatingByUserAsync(string userId, string mapId);
    public UniTask<bool> UploadMapAsync(MapMeta meta, byte[] bytes);
    public UniTask<bool> UploadMapRatingAsync(string mapId, MapRating rating);
}