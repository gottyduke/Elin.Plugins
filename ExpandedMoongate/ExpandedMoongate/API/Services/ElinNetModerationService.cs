using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Exm.Helper;
using Exm.Model.Map;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Exm.API.Services;

internal class ElinNetModerationService : ElinNetMapService, IModerationService
{
    // GET
    // /moderation/unprepared
    public async UniTask<MapMeta[]?> GetUnpreparedListAsync()
    {
        ExmMod.Log("getting unprepared map list");

        var url = $"{BaseUrl}/moderation/unprepared";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            .SetStandardHandler("application/json");
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>(
                $"failed to get unprepared map list: {req.responseCode}\n{req.downloadHandler.text}");
            return null;
        }

        var list = JsonConvert.DeserializeObject<MapMeta[]>(req.downloadHandler.text);
        ExmMod.Log("finished getting unprepared map list");
        return list;
    }

    public async UniTask<bool> DeleteMapAsync(string fileKey)
    {
        throw new NotImplementedException();
    }

    public async UniTask<bool> PostMapPreviewAsync(string previewKey, byte[] bytes)
    {
        throw new NotImplementedException();
    }

    // POST
    // /maps/upload
    // &mapId
    public async UniTask<bool> PostMapAsync(MapMeta meta, byte[] bytes)
    {
        ExmMod.Log($"uploading map '{meta.Id}'");

        var json = JsonConvert.SerializeObject(meta, Settings);
        var url = $"{BaseUrl}/maps/upload";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            .SetStandardHandler("application/json")
            .SetUploaderBytes(Encoding.UTF8.GetBytes(json))
            .SetParams(new {
                mapId = meta.Id,
            });
        await req.SendRequestEx();

        if (req.result == UnityWebRequest.Result.Success) {
            ExmMod.Log($"finished uploading map '{meta.Id}'");
            return true;
        }

        switch (req.responseCode) {
            case 409:
                // this should never be hit at runtime
                // only used by gcp job
                ExmMod.Log($"map is already present '{meta.Id}'");
                return true;
            case 424:
                // wait for file
                var surrogate = JsonConvert.DeserializeObject<UploadFileKeySurrogate>(req.downloadHandler.text, Settings);
                var success = await PostMapFileAsync(surrogate.FileKey, bytes);
                if (success) {
                    return await PostMapAsync(meta, bytes);
                }
                break;
        }

        ExmMod.WarnWithPopup<IMapService>($"failed to upload map '{meta.Id}': {req.responseCode}\n{req.downloadHandler.text}");
        return false;
    }

    // POST
    // /files/upload
    // &fileKeyId
    public async UniTask<bool> PostMapFileAsync(string fileKeyId, byte[] bytes)
    {
        ExmMod.Log($"uploading map file '{fileKeyId}'");

        var url = $"{BaseUrl}/files/upload";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            .SetStandardHandler("application/octet-stream")
            .SetUploaderBytes(bytes)
            .SetParams(new {
                fileKeyId,
            });
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>(
                $"failed to upload map file '{fileKeyId}': {req.responseCode}\n{req.downloadHandler.text}");
            return false;
        }

        ExmMod.Log($"finished uploading map file '{fileKeyId}'");
        return true;
    }

    public record UploadFileKeySurrogate(string FileKey);
}