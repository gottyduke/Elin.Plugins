using System;
using System.Text;
using Cwl.Helper.String;
using Cysharp.Threading.Tasks;
using EGate.API;
using EGate.Helper;
using EGate.Model.Map;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace EGate.Service;

public class CloudMapService(string endpoint = CloudMapService.DefaultElinModdingEndPoint) : IMapService
{
    public const string DefaultElinModdingEndPoint = "https://api-exmoongate.elin-modding.net";

    private static readonly JsonSerializerSettings _settings = new() {
        Formatting = Formatting.None,
        PreserveReferencesHandling = PreserveReferencesHandling.None,
    };

    private readonly string _baseUrl = endpoint.TrimEnd('/');

    // POST
    // /maps/upload/:mapId
    public async UniTask<bool> UploadMapAsync(MapMeta meta, byte[] bytes)
    {
        EgMod.Log($"uploading map '{meta.Id}'");

        var json = JsonConvert.SerializeObject(meta, _settings);
        var url = $"{_baseUrl}/maps/upload/{UnityWebRequest.EscapeURL(meta.Id)}";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.SetStandardHandler("application/json");
        await req.SendRequestEx();

        if (req.result == UnityWebRequest.Result.Success) {
            EgMod.Log($"finished uploading map '{meta.Id}'");
            return true;
        }

        switch (req.responseCode) {
            case 409:
                // this should never be hit at runtime
                // only used by gcp job
                EgMod.Log($"map is already present '{meta.Id}'");
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

        EgMod.Warn($"failed to upload map '{meta.Id}': {req.responseCode}\n{req.downloadHandler.text}");
        return false;
    }

    // POST
    // /ratings/:mapId
    public async UniTask<bool> UploadMapRatingAsync(string mapId, MapRating rating)
    {
        EgMod.Log($"updating map rating '{mapId}'");

        var json = JsonConvert.SerializeObject(rating, _settings);
        var url = $"{_baseUrl}/ratings";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.SetStandardHandler("application/json");
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            EgMod.Warn($"failed to rate map '{mapId}': {req.responseCode}\n{req.downloadHandler.text}");
            return false;
        }

        EgMod.Log($"finished updating map rating '{mapId}'");
        return true;
    }

    public UniTask<bool> UploadMapPreviewAsync(string mapId, byte[] bytes)
    {
        throw new NotImplementedException();
    }

    // GET
    // /maps/download/:mapId
    public async UniTask<byte[]?> GetMapFileAsync(string mapId)
    {
        EgMod.Log($"downloading map '{mapId}'");

        var url = $"{_baseUrl}/maps/download/{UnityWebRequest.EscapeURL(mapId)}";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
        req.SetStandardHandler("application/octet-stream");
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            EgMod.Warn($"failed to download map '{mapId}': {req.responseCode}\n{req.downloadHandler.text}");
            return null;
        }

        EgMod.Log($"finished downloading map '{mapId}' with {StringHelper.ToAllocateString(req.downloadHandler.data.Length)}");
        return req.downloadHandler.data;
    }

    // GET
    // /maps/query/:mapId
    public async UniTask<MapMeta?> GetMapMetaAsync(string mapId)
    {
        EgMod.Log($"querying map '{mapId}'");

        var url = $"{_baseUrl}/maps/query/{UnityWebRequest.EscapeURL(mapId)}";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
        req.downloadHandler = new DownloadHandlerBuffer();
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            EgMod.Warn($"failed to query map '{mapId}': {req.responseCode}\n{req.downloadHandler.text}");
            return null;
        }

        var meta = JsonConvert.DeserializeObject<MapMeta>(req.downloadHandler.text, _settings);
        EgMod.Log($"finished querying map '{mapId}'");
        return meta;
    }

    // GET
    // /maps/top/:sort/:count
    public async UniTask<MapMeta[]> GetTopMapsAsync(IMapService.SortType sort, int count, int offset)
    {
        var sortType = sort.ToString().ToLower();
        EgMod.Log($"getting top {count} {sortType} maps");

        var url = $"{_baseUrl}/maps/top/{sortType}/{count}/{offset}";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
        req.SetStandardHandler("application/json");
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            EgMod.Warn($"failed to get top {count} {sortType} maps: {req.responseCode}\n{req.downloadHandler.text}");
            return [];
        }

        var maps = JsonConvert.DeserializeObject<MapMeta[]>(req.downloadHandler.text, _settings);
        EgMod.Log($"finished getting top {count} {sortType} maps");
        return maps;
    }

    // GET
    // /ratings/:mapId/:count
    public async UniTask<MapRating[]> GetMapRatingsAsync(string mapId, int count)
    {
        EgMod.Log($"querying top {count} ratings of map '{mapId}'");

        var url = $"{_baseUrl}/ratings/{UnityWebRequest.EscapeURL(mapId)}/{count}";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
        req.SetStandardHandler("application/json");
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            EgMod.Warn($"failed to get top {count} ratings of map '{mapId}': {req.responseCode}\n{req.downloadHandler.text}");
            return [];
        }

        var ratings = JsonConvert.DeserializeObject<MapRating[]>(req.downloadHandler.text, _settings);
        EgMod.Log($"finished getting top {count} ratings of map '{mapId}'");
        return ratings;
    }

    // GET
    // /ratings/:mapId/:userId
    public async UniTask<MapRating?> GetMapRatingByUserAsync(string mapId, string userId)
    {
        EgMod.Log($"querying map rating '{mapId}' by '{userId}'");

        var url = $"{_baseUrl}/ratings/{UnityWebRequest.EscapeURL(mapId)}/{UnityWebRequest.EscapeURL(userId)}";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
        req.SetStandardHandler("application/json");
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            EgMod.Warn($"failed to query map rating '{mapId}' by '{userId}': {req.responseCode}\n{req.downloadHandler.text}");
            return null;
        }

        var rating = JsonConvert.DeserializeObject<MapRating?>(req.downloadHandler.text, _settings);
        EgMod.Log($"finished querying map rating '{mapId}' by '{userId}'");
        return rating;
    }

    public UniTask<byte[]?> GetMapPreviewAsync(string mapId)
    {
        throw new NotImplementedException();
    }

    // POST
    // /files/upload/:fileKey
    public async UniTask<bool> UploadMapFileAsync(string fileKey, byte[] bytes)
    {
        EgMod.Log($"uploading map file '{fileKey}'");

        var url = $"{_baseUrl}/files/upload/{UnityWebRequest.EscapeURL(fileKey)}";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.SetStandardHandler("application/octet-stream");
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            EgMod.Warn($"failed to upload map file '{fileKey}': {req.responseCode}\n{req.downloadHandler.text}");
            return false;
        }

        EgMod.Log($"finished uploading map file '{fileKey}'");
        return true;
    }

    public record UploadFileKeySurrogate(string FileKey);
}