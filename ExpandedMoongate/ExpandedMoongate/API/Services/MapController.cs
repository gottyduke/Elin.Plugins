using System;
using System.IO;
using System.Net;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cysharp.Threading.Tasks;
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

        Directory.CreateDirectory(CorePath.ZoneSaveUser);

        var filePath = Path.Combine(CorePath.ZoneSaveUser, meta.Id.SanitizeFileName());
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
        ExmMod.Log<MapController>($"cloud map '{meta.Id}' saved at {filePath.ShortPath()}");

        core.actionsNextFrame.Add(() => MoveMap(meta, filePath));

        return true;
    }

    public void MoveMap(MapMeta meta, string path)
    {
        var elinMeta = Map.GetMetaData(path);
        if (elinMeta is null) {
            ExmMod.WarnWithPopup<MapController>("exm_error_elin_meta_failure".Loc(meta.Id));
            return;
        }

        if (pc.currentZone is Zone_User) {
            pc.MoveZone(pc.homeZone);
            core.actionsNextFrame.Add(() => MoveMap(meta, path));
            return;
        }

        ExmMod.Log<MapController>($"loading cloud map\n{meta.TryToString()}");

        if (SpatialGen.Create("user", world.region, true) is not Zone_User userMap) {
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

        pc.SetFlagValue("on_moongate");
        pc.MoveZone(userMap, ZoneTransition.EnterState.Moongate);
    }

    private class ZoneRatingUpdateEvent(IMapService service, MapMeta meta) : ZoneEvent
    {
        public override void OnLeaveZone()
        {
            pc.SetFlagValue("on_moongate", 0);

            CoroutineHelper.Deferred(() =>
                Dialog.YesNo(
                    "exm_ui_rate_map_dialog".Loc(WebUtility.HtmlDecode(meta.Title)),
                    () => UpdateRating(true),
                    () => UpdateRating(false),
                    "exm_ui_rate_map_dialog_yes"));
        }

        public override void OnVisit()
        {
            ui.Say(WebUtility.HtmlDecode("exm_ui_visit_map".Loc(meta.Title, meta.Author)));
        }

        public void UpdateRating(bool like)
        {
            UpdateRatingAsync().ForgetEx();

            return;

            async UniTask UpdateRatingAsync()
            {
                await service.PostMapRatingAsync(meta.Id, new() {
                    MapId = meta.Id,
                    UserId = SteamUser.GetSteamID().ToString(),
                    RatedAt = like ? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") : null,
                });
            }
        }
    }
}