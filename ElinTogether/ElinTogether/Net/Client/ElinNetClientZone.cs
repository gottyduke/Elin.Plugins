using ElinTogether.Common;
using ElinTogether.Models;
using ElinTogether.Patches;
using ReflexCLI.Attributes;
using Serilog.Context;

namespace ElinTogether.Net;

[ConsoleCommandClassCustomizer("emp")]
internal partial class ElinNetClient
{
    /// <summary>
    ///     Request a map snapshot manually, mostly just used when joining a session
    /// </summary>
    public void RequestZoneState(MapDataRequest request)
    {
        EmpLog.Information("Requesting zone state {@Zone} from host",
            request);

        Host.Send(request);
    }

    /// <summary>
    ///     Net event: Received zone state update, must do a scene init
    /// </summary>
    private void OnZoneDataResponse(ZoneDataResponse response)
    {
        using var _ = LogContext.PushProperty("Zone", new { response.ZoneFullName, response.ZoneUid }, true);

        response.WriteToTemp();

        var spatial = game.spatials;

        // popping from spatial gen refs makes no sense actually
        var remoteZone = spatial.Find(response.ZoneUid) ?? SpatialGenEvent.TryPop(response.ZoneUid);
        if (remoteZone is null) {
            EmpLog.Information("Remote zone does not exist, waiting for new spatial gen");

            var probeZone = response.Zone.Decompress<Zone>();
            var parent = spatial.Find(probeZone.parent.uid);

            if (parent is null) {
                EmpLog.Warning("Remote zone parent does not exist in current game");

                // TODO: add reconnect logic to resync save probe
                Socket.Disconnect(Host, EmpDisconnectInfo.InvalidZone);
                return;
            }

            // swap out the serialized parent
            probeZone.parent = parent;
            spatial.map[response.ZoneUid] = remoteZone = probeZone;

            if (parent is Region region) {
                region.elomap.SetZone(probeZone.x, probeZone.y, remoteZone, true);
            }
        }

        if (remoteZone.ZoneFullName != response.ZoneFullName) {
            EmpLog.Warning("Zone state mismatch");

            // TODO: add reconnect logic to resync save probe
            Socket.Disconnect(Host, EmpDisconnectInfo.InvalidZone);
            return;
        }

        EmpLog.Information("Received zone state");

        // suppress client-side map regeneration
        remoteZone.isGenerated = true;
        remoteZone.dateExpire = int.MaxValue;

        if (remoteZone is Region eloMap) {
            // suppress client-side overworld poi generation
            eloMap.dateCheckSites = int.MaxValue;
        }

        // update session remote zone
        Session.CurrentZone = remoteZone;

        // respond for replication complete, waiting for sync position
        Host.Send(response.Ready());
    }

    /// <summary>
    ///     Net event: Ready to init scene with new zone state and sync position
    /// </summary>
    private void OnZoneActivateResponse(ZoneActivateResponse response)
    {
        var currentZone = Session.CurrentZone;

        // Staleness guard (Phase 1): use ZoneFullName + ZoneUid instead of hard disconnect.
        // If mismatch, the client will receive the correct zone data later via snapshot/delta or re-request.
        if (currentZone?.uid != response.ZoneUid || currentZone.ZoneFullName != response.ZoneFullName) {
            EmpLog.Debug("Ignoring stale ZoneActivateResponse (expected {ExpectedName}/{ExpectedUid}, got {GotName}/{GotUid})",
                currentZone?.ZoneFullName, currentZone?.uid,
                response.ZoneFullName, response.ZoneUid);
            return;
        }

        // Unified transition (Phase 2): single path, no player.zone == null hack.
        if (player.zone != currentZone) {
            player.zone = pc.currentZone = currentZone;
            if (!core.IsGameStarted || scene.mode != Scene.Mode.Zone) {
                // first time or coming from title
                scene.Init(Scene.Mode.Zone);
            } else {
                // normal in-game zone change
                pc.MoveZone(currentZone);
            }
        }

        // reassign zone pos
        this.StartDeferredCoroutine(() => pc.Stub_Move(response.Pos, Card.MoveType.Force));
    }
}