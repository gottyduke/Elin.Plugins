using System;
using System.Collections.Generic;
using System.Linq;
using ElinTogether.Helper;
using ElinTogether.Models;
using ElinTogether.Net.Steam;

namespace ElinTogether.Net;

internal partial class ElinNetHost
{
    private readonly Dictionary<uint, Dictionary<SourceListType, bool>> _validationResults = [];

    internal readonly HashSet<SourceListType> SourceValidationsEnabled = [];

    private void RequestSourceValidation(ISteamNetPeer peer)
    {
        _validationResults.Remove(peer.Id);

        peer.Send(new SourceListRequest {
            Type = SourceListType.All,
        });

        EmpLog.Debug("Requesting source lists validation from player {@Peer}", peer);
    }

    private void OnSourceListResponse(SourceListResponse response, ISteamNetPeer peer)
    {
        if (!_validationResults.TryGetValue(peer.Id, out var validations)) {
            validations = _validationResults[peer.Id] = [];
        }

        SourceValidation.ThrowIfInvalid(response.Type);

        var valid = response.Checksum.SequenceEqual(SourceList[response.Type]);
        validations[response.Type] = valid;

        EmpLog.Information("Received source list validation {SourceListType} from player {@Peer}",
            response.Type, peer);

        if (valid) {
            if (validations.Count != SourceList.Count || !validations.Values.All(r => r)) {
                return;
            }

            EmpLog.Information("Player {@Peer} has validated all source lists",
                peer);

            PreparePlayerJoin(peer);
        } else {
            EmpLog.Information("Sending source list diff {SourceListType} to player {@Peer}",
                response.Type, peer);

            peer.Send(new SourceDiffResponse {
                Type = response.Type,
                IdList = SourceValidation.GenerateSourceIdList(response.Type).ToArray(),
            });
        }
    }
}