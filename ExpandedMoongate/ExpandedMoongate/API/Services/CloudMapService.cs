using System;
using System.Text;
using Cwl.Helper.String;
using Cysharp.Threading.Tasks;
using Exm.Helper;
using Exm.Model.Map;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Exm.API.Services;

public class CloudMapService(string endpoint = CloudMapService.DefaultElinModdingEndPoint) : IMapService
{
    public const string DefaultElinModdingEndPoint = "https://api.exmoongate.elin-modding.net";

    private static readonly JsonSerializerSettings _settings = new() {
        Formatting = Formatting.None,
        PreserveReferencesHandling = PreserveReferencesHandling.None,
    };

    private readonly string _baseUrl = endpoint.TrimEnd('/');

    // POST
    // /maps/overview
    public async UniTask<MapServiceOverview?> GetMapsOverviewAsync()
    {
        ExmMod.Log("querying map server overview");

        var url = $"{_baseUrl}/maps/overview";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            .SetStandardHandler("application/json");
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>(
                $"failed to query map server overview: {req.responseCode}\n{req.downloadHandler.text}");
            return null;
        }

        var overview = JsonConvert.DeserializeObject<MapServiceOverview?>(req.downloadHandler.text, _settings);
        ExmMod.Log("finished querying map server overview");
        return overview;
    }

    public record UploadFileKeySurrogate(string FileKey);

    #region Map Meta

    // POST
    // /maps/upload/:mapId
    public async UniTask<bool> UploadMapAsync(MapMeta meta, byte[] bytes)
    {
        ExmMod.Log($"uploading map '{meta.Id}'");

        var json = JsonConvert.SerializeObject(meta, _settings);
        var url = $"{_baseUrl}/maps/upload/{Uri.EscapeDataString(meta.Id)}";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            .SetStandardHandler("application/json")
            .SetUploaderBytes(Encoding.UTF8.GetBytes(json));
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
                var surrogate = JsonConvert.DeserializeObject<UploadFileKeySurrogate>(req.downloadHandler.text, _settings);
                var success = await UploadMapFileAsync(surrogate.FileKey, bytes);
                if (success) {
                    return await UploadMapAsync(meta, bytes);
                }
                break;
        }

        ExmMod.WarnWithPopup<IMapService>($"failed to upload map '{meta.Id}': {req.responseCode}\n{req.downloadHandler.text}");
        return false;
    }

    // GET
    // /maps/query/:mapId
    public async UniTask<MapMeta?> GetMapMetaAsync(string mapId)
    {
        ExmMod.Log($"querying map '{mapId}'");

        var url = $"{_baseUrl}/maps/query/{Uri.EscapeDataString(mapId)}";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            .SetStandardHandler("application/json");
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>($"failed to query map '{mapId}': {req.responseCode}\n{req.downloadHandler.text}");
            return null;
        }

        var meta = JsonConvert.DeserializeObject<MapMeta>(req.downloadHandler.text, _settings);
        ExmMod.Log($"finished querying map '{mapId}'");
        return meta;
    }

    // GET
    // /maps/top
    // &sort=[created|rating|visits]
    // &limit=[10, 300]
    // &page=[0]
    // &lang=[null|EN|JP|CN]
    // &noTags=[adult]
    // &version
    public async UniTask<MapMeta[]> GetTopMapsAsync(IMapServiceV1.SortType sort,
                                                    int count,
                                                    int page,
                                                    string? lang,
                                                    string? noTags)
    {
        var sortType = sort.ToString().ToLower();
        ExmMod.Log($"getting top {count} {sortType} maps");

        var url = $"{_baseUrl}/maps/top";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            .SetStandardHandler("application/json")
            .SetParams(new {
                sort = sort.ToString().ToLowerInvariant(),
                count,
                page,
                lang,
                noTags,
                version = GameVersion.Int(),
            });
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>(
                $"failed to get top {count} {sortType} maps: {req.responseCode}\n{req.downloadHandler.text}");
            return [];
        }

        var maps = JsonConvert.DeserializeObject<MapMeta[]>(req.downloadHandler.text, _settings);
        ExmMod.Log($"finished getting top {count} {sortType} maps");
        return maps;
    }

    #endregion

    #region Map File

    // POST
    // /files/upload/:fileKey
    public async UniTask<bool> UploadMapFileAsync(string fileKey, byte[] bytes)
    {
        ExmMod.Log($"uploading map file '{fileKey}'");

        var url = $"{_baseUrl}/files/upload/{Uri.EscapeDataString(fileKey)}";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            .SetStandardHandler("application/octet-stream")
            .SetUploaderBytes(bytes);
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>(
                $"failed to upload map file '{fileKey}': {req.responseCode}\n{req.downloadHandler.text}");
            return false;
        }

        ExmMod.Log($"finished uploading map file '{fileKey}'");
        return true;
    }

    // GET
    // /maps/download/:mapId
    public async UniTask<byte[]?> GetMapFileAsync(string mapId)
    {
        ExmMod.Log($"downloading map '{mapId}'");

        var url = $"{_baseUrl}/maps/download/{Uri.EscapeDataString(mapId)}";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            .SetStandardHandler("application/octet-stream");
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>(
                $"failed to download map '{mapId}': {req.responseCode}\n{req.downloadHandler.text}");
            return null;
        }

        ExmMod.Log($"finished downloading map '{mapId}' with {StringHelper.ToAllocateString(req.downloadHandler.data.Length)}");
        return req.downloadHandler.data;
    }

    #endregion

    #region Map Preview

    public UniTask<bool> UploadMapPreviewAsync(string mapId, byte[] bytes)
    {
        throw new NotImplementedException();
    }

    public UniTask<byte[]?> GetMapPreviewAsync(string mapId)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Map Rating

    // POST
    // /ratings/:mapId
    public async UniTask<bool> UploadMapRatingAsync(string mapId, MapRating rating)
    {
        ExmMod.Log($"updating map rating '{mapId}'");

        var json = JsonConvert.SerializeObject(rating, _settings);
        var url = $"{_baseUrl}/ratings";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            .SetStandardHandler("application/json")
            .SetUploaderBytes(Encoding.UTF8.GetBytes(json));
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>($"failed to rate map '{mapId}': {req.responseCode}\n{req.downloadHandler.text}");
            return false;
        }

        ExmMod.Log($"finished updating map rating '{mapId}'");
        return true;
    }

    // GET
    // /ratings/:userId/:mapId
    public async UniTask<MapRating?> GetMapRatingByUserAsync(string userId, string mapId)
    {
        ExmMod.Log($"querying user rating '{userId}' for '{mapId}'");

        var url = $"{_baseUrl}/ratings/{Uri.EscapeDataString(userId)}/{Uri.EscapeDataString(mapId)}";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            .SetStandardHandler("application/json");
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>(
                $"failed to query map rating '{mapId}' by '{userId}': {req.responseCode}\n{req.downloadHandler.text}");
            return null;
        }

        var rating = JsonConvert.DeserializeObject<MapRating?>(req.downloadHandler.text, _settings);
        ExmMod.Log($"finished querying user rating '{userId}' for '{mapId}'");
        return rating;
    }

    #endregion
}