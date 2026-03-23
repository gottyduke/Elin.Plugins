using Cysharp.Threading.Tasks;
using Exm.Model.Map;

namespace Exm.API;

public interface IMapService
{
    public enum SortType
    {
        Created,
        Rating,
        Visits,
    }

    public UniTask<byte[]?> GetMapFileAsync(string mapId);
    public UniTask<MapMeta?> GetMapMetaAsync(string mapId);
    public UniTask<byte[]?> GetMapPreviewAsync(string mapId);
    public UniTask<MapMeta[]> GetTopMapsAsync(SortType sort, int limit, int page = 0);
    public UniTask<MapRating[]> GetMapRatingsAsync(string mapId, int limit, int page = 0);
    public UniTask<MapRating?> GetMapRatingByUserAsync(string userId, string mapId);
    public UniTask<bool> UploadMapAsync(MapMeta meta, byte[] bytes);
    public UniTask<bool> UploadMapPreviewAsync(string mapId, byte[] bytes);
    public UniTask<bool> UploadMapRatingAsync(string mapId, MapRating rating);
}