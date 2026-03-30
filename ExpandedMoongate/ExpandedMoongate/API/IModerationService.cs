using Cysharp.Threading.Tasks;
using Exm.Model.Map;

namespace Exm.API;

public interface IModerationService : IMapService
{
    public UniTask<MapMeta[]?> GetUnpreparedListAsync();
    public UniTask<bool> DeleteMapAsync(string fileKey);
    public UniTask<bool> PostMapAsync(MapMeta meta, byte[] bytes);
    public UniTask<bool> PostMapFileAsync(string fileKey, byte[] bytes);
    public UniTask<bool> PostMapPreviewAsync(string previewKey, byte[] bytes);
}