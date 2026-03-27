using System;
using System.IO;
using System.Linq;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cysharp.Threading.Tasks;
using Exm.Components;
using Exm.Model.Map;
using Steamworks;

namespace Exm.API.Services;

public class MapController(IMapService service) : EClass
{
    public void LoadMap(MapMeta meta)
    {
        if (pc.burden.GetPhase() == StatsBurden.Squashed) {
            Msg.Say("returnOverweight");
            return;
        }

        var filePath = Path.Combine(CorePath.ZoneSaveUser, meta.FileKey.SanitizeFileName());
        if (!File.Exists(filePath)) {
            LayerProgress.StartAsync("Downloading", DownloadMap(meta, filePath));
        } else {
            MoveMap(meta, filePath);
        }

    }

    public async UniTask<bool> DownloadMap(MapMeta meta, string filePath)
    {
        var blob = await service.GetMapFileAsync(meta.Id);
        if (blob is not { Length: > 0 }) {
            return false;
        }

        await UniTask.Yield();
        await File.WriteAllBytesAsync(filePath, blob);
        ExmMod.Log<MapController>("exm_log_map_saved".Loc(meta.Id, filePath.ShortPath()));

        core.actionsNextFrame.Add(() => MoveMap(meta, filePath));

        return true;
    }

    public void MoveMap(MapMeta meta, string path)
    {
        CleanupUserMaps();

        var elinMeta = Map.GetMetaData(path);
        if (elinMeta is null) {
            ExmMod.WarnWithPopup<MapController>("exm_error_elin_meta_failure".Loc(meta.Id));
            return;
        }

        ExmMod.Log<MapController>("exm_log_map_loading".Loc(meta.TryToString()));

        var userMap = SpatialGen.Create("user", world.region, true) as Zone_User;
        if (userMap is null) {
            ExmMod.WarnWithPopup<MapController>("exm_error_zone_user".Loc(meta.Id));
            return;
        }

        userMap.path = elinMeta.path;
        userMap.idUser = elinMeta.id;
        userMap.dateExpire = world.date.GetRaw(1);
        userMap.name = elinMeta.name;
        if (elinMeta.underwater) {
            userMap.elements.SetBase(FACTION.bfUndersea, 1);
        }

        var returnPos = pc.pos;
        userMap.instance = new ZoneInsstanceMoongate {
            uidZone = _zone.uid,
            x = returnPos.x,
            z = returnPos.z,
        };

        userMap.events.Add(new ZoneRatingUpdateEvent(service, meta));

        pc.MoveZone(userMap, ZoneTransition.EnterState.Moongate);
    }

    public void CleanupUserMaps()
    {
        var userMaps = game.spatials.map.Values
            .OfType<Zone_User>()
            .ToArray();
        foreach (var userMap in userMaps) {
            game.spatials.Remove(userMap);
        }
    }

    private class ZoneRatingUpdateEvent(IMapService service, MapMeta meta) : ZoneEvent
    {
        public override void OnLeaveZone()
        {
            Dialog.YesNo("rate map?",
                () => UpdateRating(true),
                () => UpdateRating(false));
        }

        public void UpdateRating(bool like)
        {
            UpdateRatingAsync().ForgetEx();

            return;

            async UniTask UpdateRatingAsync()
            {
                var success = await service.UploadMapRatingAsync(meta.Id, new() {
                    MapId = meta.Id,
                    UserId = SteamUser.GetSteamID().ToString(),
                    RatedAt = like ? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") : null,
                });

                if (success) {

                } else {
                    ExmMod.WarnWithPopup<MapCardView>("exm_ui_map_rate_update_failed".lang());
                }
            }
        }
    }
}