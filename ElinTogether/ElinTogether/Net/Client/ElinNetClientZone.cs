using ElinTogether.Common;
using ElinTogether.Helper.Extensions;
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

        EmpLog.Information("Received zone state");

        response.WriteToTemp();

        var spatial = game.spatials;

        // popping from spatial gen refs makes no sense actually
        var remoteZone = response.FindZone();
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

        // suppress client-side map regeneration
        remoteZone.isGenerated = true;
        remoteZone.dateExpire = int.MaxValue;

        if (remoteZone is Region eloMap) {
            // suppress client-side overworld poi generation
            eloMap.dateCheckSites = int.MaxValue;
        }

        // update session remote zone
        Session.CurrentZone = remoteZone;

        // respond for replication complete, waiting for position sync
        Host.Send(response.Ready());
    }

    /// <summary>
    ///     Net event: Ready to init scene with new zone state and sync position
    /// </summary>
    private void OnZoneActivateResponse(ZoneActivateResponse response)
    {
        using var _ = LogContext.PushProperty("Zone", new { response.ZoneFullName, response.ZoneUid }, true);

        EmpLog.Information("Received zone activation");

        var currentZone = Session.CurrentZone;

        if (currentZone?.uid != response.ZoneUid) {
            EmpLog.Debug("Ignoring stale ZoneActivateResponse (expected {ExpectedName}/{ExpectedUid}, got {GotName}/{GotUid})",
                currentZone?.ZoneFullName, currentZone?.uid, response.ZoneFullName, response.ZoneUid);
            return;
        }

        if (player.zone is null) {
            // first time joining, need to do scene init from title
            EmpLog.Debug("Starting initial scene init");

            player.zone = pc.currentZone = currentZone;
            scene.Init(Scene.Mode.Zone);
        } else if (player.zone != currentZone) {
            // do normal zone transition
            pc.MoveZone(currentZone);
        }

        // reassign zone pos
        this.StartDeferredCoroutine(() => pc.Stub_Move(response.Pos, Card.MoveType.Force));
    }
}