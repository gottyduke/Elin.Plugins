using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exm.Model.Map;

namespace Exm.API.Services;

public class CollectiveMapService : IMapService
{
    private readonly HashSet<IMapService> _services = [];

    public async UniTask<byte[]?> GetMapFileAsync(string mapId)
    {
        foreach (var service in _services) {
            try {
                var result = await service.GetMapFileAsync(mapId);
                if (result is { Length: > 0 }) {
                    return result;
                }
            } catch (Exception ex) {
                ExmMod.WarnWithPopup<IMapService>($"{service.GetType().Name} failed\n{ex}");
                // noexcept
            }
        }
        return null;
    }

    public async UniTask<MapMeta?> GetMapMetaAsync(string mapId)
    {
        foreach (var service in _services) {
            try {
                var result = await service.GetMapMetaAsync(mapId);
                if (result is not null) {
                    return result;
                }
            } catch (Exception ex) {
                ExmMod.WarnWithPopup<IMapService>($"{service.GetType().Name} failed\n{ex}");
                // noexcept
            }
        }
        return null;
    }

    public async UniTask<MapMeta[]?> GetTopMapsAsync(IMapServiceV1.SortType sort,
                                                     int count,
                                                     int page,
                                                     IMapServiceV1.LangFilter lang,
                                                     IMapServiceV1.TimePeriod days,
                                                     string? noTags)
    {
        foreach (var service in _services) {
            try {
                var result = await service.GetTopMapsAsync(sort, count, page, lang, days, noTags);
                if (result is { Length: > 0 }) {
                    return result;
                }
            } catch (Exception ex) {
                ExmMod.WarnWithPopup<IMapService>($"{service.GetType().Name} failed\n{ex}");
                // noexcept
            }
        }
        return null;
    }

    public async UniTask<MapRating?> GetMapRatingByUserAsync(string mapId, string userId)
    {
        foreach (var service in _services) {
            try {
                var result = await service.GetMapRatingByUserAsync(mapId, userId);
                if (result is not null) {
                    return result;
                }
            } catch (Exception ex) {
                ExmMod.WarnWithPopup<IMapService>($"{service.GetType().Name} failed\n{ex}");
                // noexcept
            }
        }
        return null;
    }

    public async UniTask<bool> PostMapRatingAsync(string mapId, MapRating rating)
    {
        foreach (var service in _services) {
            try {
                var result = await service.PostMapRatingAsync(mapId, rating);
                if (result) {
                    return result;
                }
            } catch (Exception ex) {
                ExmMod.WarnWithPopup<IMapService>($"{service.GetType().Name} failed\n{ex}");
                // noexcept
            }
        }
        return false;
    }

    public async UniTask<MapMeta[]?> GetMapHistoryByUserAsync(string userId)
    {
        List<MapMeta> results = [];
        foreach (var service in _services) {
            try {
                var result = await service.GetMapHistoryByUserAsync(userId);
                if (result is { Length: > 0 }) {
                    results.AddRange(result);
                }
            } catch (Exception ex) {
                ExmMod.WarnWithPopup<IMapService>($"{service.GetType().Name} failed\n{ex}");
                // noexcept
            }
        }
        return results.ToArray();
    }

    public async UniTask<MapMeta[]?> GetMapMetaByQueryAsync(string query)
    {
        List<MapMeta> results = [];
        foreach (var service in _services) {
            try {
                var result = await service.GetMapMetaByQueryAsync(query);
                if (result is { Length: > 0 }) {
                    results.AddRange(result);
                }
            } catch (Exception ex) {
                ExmMod.WarnWithPopup<IMapService>($"{service.GetType().Name} failed\n{ex}");
                // noexcept
            }
        }
        return results.ToArray();
    }

    public async UniTask<byte[]?> GetMapPreviewAsync(string mapId)
    {
        foreach (var service in _services) {
            try {
                var result = await service.GetMapPreviewAsync(mapId);
                if (result is { Length: > 0 }) {
                    return result;
                }
            } catch (Exception ex) {
                ExmMod.WarnWithPopup<IMapService>($"{service.GetType().Name} failed\n{ex}");
                // noexcept
            }
        }
        return null;
    }

    /*
    public async UniTask<byte[]?> GetMapPreviewAsync(string mapId)
    {
        foreach (var service in _services) {
            try {
                var result = await service.GetMapPreviewAsync(mapId);
                if (result is { Length: > 0 }) {
                    return result;
                }
            } catch (Exception ex) {
                ExmMod.WarnWithPopup<IMapService>($"{service.GetType().Name} failed\n{ex}");
                // noexcept
            }
        }
        return null;
    }

    public async UniTask<bool> UploadMapPreviewAsync(string mapId, byte[] bytes)
    {
        foreach (var service in _services) {
            try {
                var result = await service.UploadMapPreviewAsync(mapId, bytes);
                if (result) {
                    return result;
                }
            } catch (Exception ex) {
                ExmMod.WarnWithPopup<IMapService>($"{service.GetType().Name} failed\n{ex}");
                // noexcept
            }
        }
        return false;
    }
    /**/

    public async UniTask<MapServiceOverview?> GetMapsOverviewAsync(IMapServiceV1.LangFilter lang,
                                                                   IMapServiceV1.TimePeriod days,
                                                                   string? noTags)
    {
        foreach (var service in _services) {
            try {
                var result = await service.GetMapsOverviewAsync(lang, days, noTags);
                if (result is not null) {
                    return result;
                }
            } catch (Exception ex) {
                ExmMod.WarnWithPopup<IMapService>($"{service.GetType().Name} failed\n{ex}");
                // noexcept
            }
        }
        return null;
    }

    public void Add(IMapService service)
    {
        _services.Add(service);
    }

    public void Remove(IMapService service)
    {
        _services.Remove(service);
    }
}