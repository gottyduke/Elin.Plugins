using System;
using System.Text;
using Cwl.Helper.String;
using Cysharp.Threading.Tasks;
using Exm.Helper;
using Exm.Model.Map;
using Newtonsoft.Json;
using Steamworks;
using UnityEngine.Networking;

namespace Exm.API.Services;

public class ElinNetMapService(string endpoint = ElinNetMapService.DefaultElinModdingEndPoint) : IMapService
{
    public const string DefaultElinModdingEndPoint = "https://api.exmoongate.elin-modding.net";

    protected static readonly JsonSerializerSettings Settings = new() {
        Formatting = Formatting.None,
        PreserveReferencesHandling = PreserveReferencesHandling.None,
    };

    public string BaseUrl { get; } = endpoint.TrimEnd('/');

    // POST
    // /maps/overview
    public async UniTask<MapServiceOverview?> GetMapsOverviewAsync(IMapServiceV1.LangFilter lang,
                                                                   IMapServiceV1.TimePeriod days,
                                                                   string? noTags)
    {
        ExmMod.Log("querying map server overview");

        var url = $"{BaseUrl}/maps/overview";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            .SetStandardHandler("application/json")
            .SetParams(new {
                lang = lang.ToString(),
                noTags,
                days = (int)days,
                version = GameVersion.Int(),
            });
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>(
                $"failed to query map server overview: {req.responseCode}\n{req.downloadHandler.text}");
            return null;
        }

        var overview = JsonConvert.DeserializeObject<MapServiceOverview>(req.downloadHandler.text, Settings);
        ExmMod.Log("finished querying map server overview");
        return overview;
    }

    // POST
    // /maps/search
    // &query
    public async UniTask<MapMeta[]?> GetMapMetaByQueryAsync(string query)
    {
        ExmMod.Log($"querying maps meta by '{query}'");

        var url = $"{BaseUrl}/maps/search";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            .SetStandardHandler("application/json")
            .SetParams(new {
                query,
                userId = SteamUser.GetSteamID(),
                version = GameVersion.Int(),
            });
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>(
                $"failed to query maps: {req.responseCode}\n{req.downloadHandler.text}");
            return null;
        }

        var overview = JsonConvert.DeserializeObject<MapMeta[]>(req.downloadHandler.text, Settings);
        ExmMod.Log("finished querying maps");
        return overview;
    }

    #region Map File

    // GET
    // /maps/download
    // &mapId
    public async UniTask<byte[]?> GetMapFileAsync(string mapId)
    {
        ExmMod.Log($"downloading map '{mapId}'");

        var url = $"{BaseUrl}/maps/download";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            .SetStandardHandler("application/octet-stream")
            .SetParams(new {
                mapId,
            });
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

    #region Map Meta

    // GET
    // /maps/query
    // &mapId
    public async UniTask<MapMeta?> GetMapMetaAsync(string mapId)
    {
        ExmMod.Log($"querying map '{mapId}'");

        var url = $"{BaseUrl}/maps/query";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            .SetStandardHandler("application/json")
            .SetParams(new {
                mapId,
            });
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>($"failed to query map '{mapId}': {req.responseCode}\n{req.downloadHandler.text}");
            return null;
        }

        var meta = JsonConvert.DeserializeObject<MapMeta>(req.downloadHandler.text, Settings);
        ExmMod.Log($"finished querying map '{mapId}'");
        return meta;
    }

    // GET
    // /maps/history
    // &userId
    public async UniTask<MapMeta[]?> GetMapHistoryByUserAsync(string userId)
    {
        ExmMod.Log($"querying user history for '{userId}'");

        var url = $"{BaseUrl}/maps/history";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            .SetStandardHandler("application/json")
            .SetParams(new {
                userId,
            });
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>(
                $"failed to query user map history by '{userId}': {req.responseCode}\n{req.downloadHandler.text}");
            return null;
        }

        var history = JsonConvert.DeserializeObject<MapMeta[]>(req.downloadHandler.text, Settings);
        ExmMod.Log($"finished querying user rating '{userId}'");
        return history;
    }


    // GET
    // /maps/top
    // &sort=[created|rating|visits]
    // &limit=[10, 300]
    // &page=[0]
    // &lang=[null|EN|JP|CN]
    // &noTags=[adult]
    // &version
    public async UniTask<MapMeta[]?> GetTopMapsAsync(IMapServiceV1.SortType sort,
                                                     int count,
                                                     int page,
                                                     IMapServiceV1.LangFilter lang,
                                                     IMapServiceV1.TimePeriod days,
                                                     string? noTags)
    {
        var sortType = sort.ToString().ToLower();
        ExmMod.Log($"getting top {count} {sortType} maps");

        var url = $"{BaseUrl}/maps/top";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            .SetStandardHandler("application/json")
            .SetParams(new {
                count,
                page,
                sort = sort.ToString().ToLowerInvariant(),
                lang,
                days = (int)days,
                noTags,
                userId = SteamUser.GetSteamID(),
                version = GameVersion.Int(),
            });
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>(
                $"failed to get top {count} {sortType} maps: {req.responseCode}\n{req.downloadHandler.text}");
            return null;
        }

        var maps = JsonConvert.DeserializeObject<MapMeta[]>(req.downloadHandler.text, Settings);
        ExmMod.Log($"finished getting top {count} {sortType} maps");
        return maps;
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
    // /ratings
    // &mapId
    public async UniTask<bool> PostMapRatingAsync(string mapId, MapRating rating)
    {
        ExmMod.Log($"updating map rating '{mapId}'");

        var json = JsonConvert.SerializeObject(rating, Settings);
        var url = $"{BaseUrl}/ratings";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            .SetStandardHandler("application/json")
            .SetUploaderBytes(Encoding.UTF8.GetBytes(json))
            .SetParams(new {
                mapId,
            });
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>($"failed to rate map '{mapId}': {req.responseCode}\n{req.downloadHandler.text}");
            return false;
        }

        ExmMod.Log($"finished updating map rating '{mapId}'");
        return true;
    }

    // GET
    // /ratings
    // &userId
    // &mapId
    public async UniTask<MapRating?> GetMapRatingByUserAsync(string mapId, string userId)
    {
        ExmMod.Log($"querying user rating '{userId}' for '{mapId}'");

        var url = $"{BaseUrl}/ratings";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            .SetStandardHandler("application/json")
            .SetParams(new {
                mapId,
                userId,
            });
        await req.SendRequestEx();

        if (req.result != UnityWebRequest.Result.Success) {
            ExmMod.WarnWithPopup<IMapService>(
                $"failed to query map rating '{mapId}' by '{userId}': {req.responseCode}\n{req.downloadHandler.text}");
            return null;
        }

        var rating = JsonConvert.DeserializeObject<MapRating>(req.downloadHandler.text, Settings);
        ExmMod.Log($"finished querying user rating '{userId}' for '{mapId}'");
        return rating;
    }

    #endregion
}