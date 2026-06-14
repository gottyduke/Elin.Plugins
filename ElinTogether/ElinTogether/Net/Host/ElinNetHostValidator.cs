using System.Collections.Generic;
using ElinTogether.Common;
using ElinTogether.Models;
using ElinTogether.Net.Steam;

namespace ElinTogether.Net;

internal partial class ElinNetHost
{
    private void RequestSourceValidation(ISteamNetPeer peer)
    {
        Session.SetPhase(ConnectionPhase.SourceValidating);
        EmpLog.Debug("Requesting source validation from {@Peer}",
            peer);

        var request = new SourceValidationRequest {
            SourceNames = GetValidationSourceNames(),
        };

        peer.Send(request);
    }

    private void OnSourceListResponse(SourceValidationResponse response, ISteamNetPeer peer)
    {
        EmpLog.Debug("Received source validation response from {@Peer}",
            peer);

        var mismatches = new Dictionary<string, string>();

        foreach (var (source, sha) in SourceList) {
            if (!response.Checksums.TryGetValue(source, out var clientSha) ||
                clientSha != sha) {
                mismatches[source] = sha;
            }
        }

        // ok

        // TODO: localization syncs
        var noSync = true;
        if (mismatches.Count == 0 || noSync) {
            EmpLog.Debug("Source validation passed for {@Peer}",
                peer);
            Session.SetPhase(ConnectionPhase.SourceSynced);

            if (_pendingRejoinIntents.Remove(peer.Uid, out var rejoinReq)) {
                HandleRejoin(peer, rejoinReq);
            } else {
                PreparePlayerJoin(peer);
            }

            return;
        }

        // u shall not pass

        EmpLog.Warning("Source validation mismatch: {Count} sources differ for {@Peer}",
            mismatches.Count, peer);

        var diff = new Dictionary<string, LZ4Bytes>();
        foreach (var sourceType in mismatches.Keys) {
            var source = ModUtil.FindSourceByName(sourceType);
            if (source is not null) {
                diff[sourceType] = LZ4Bytes.Create(source.ExportRows());
            }
        }

        if (diff.Count > 0) {
            EmpLog.Information("Sending source diff ({Count} sources) to {@Peer}", diff.Count,
                peer);
            // clients apply, recompute, then send another SourceValidationResponse
            peer.Send(new SourceListSync {
                SourceRows = diff,
            });
        } else {
            EmpLog.Error("Source mismatch but no rows to sync for {@Peer}. Disconnecting.",
                peer);
            Socket.Disconnect(peer, EmpDisconnectInfo.InvalidSource);
        }
    }
}