using Cysharp.Threading.Tasks;
using EGate.Model.Map;

namespace EGate.API;

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
    public UniTask<MapMeta[]> GetTopMapsAsync(SortType sort, int count, int offset);
    public UniTask<MapRating[]> GetMapRatingsAsync(string mapId, int count);
    public UniTask<MapRating?> GetMapRatingByUserAsync(string mapId, string userId);
    public UniTask<bool> UploadMapAsync(MapMeta meta, byte[] bytes);
    public UniTask<bool> UploadMapPreviewAsync(string mapId, byte[] bytes);
    public UniTask<bool> UploadMapRatingAsync(string mapId, MapRating rating);
}