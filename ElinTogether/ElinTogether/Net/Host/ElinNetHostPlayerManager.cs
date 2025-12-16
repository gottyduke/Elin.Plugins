using System.Collections.Generic;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.Extensions;
using ElinTogether.Helper;
using ElinTogether.Models;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net.Steam;
using Newtonsoft.Json;
using UnityEngine;

namespace ElinTogether.Net;

internal partial class ElinNetHost
{
    public readonly Dictionary<int, Chara> ActiveRemoteCharas = [];

    /// <summary>
    ///     A combination of all remote chara's act states
    /// </summary>
    public int SharedActState => States.Values.Sum(s => s.LastAct);

    /// <summary>
    ///     Shared speed of all players
    /// </summary>
    public int SharedSpeed => (int)States.Values.Average(s => s.Speed);

    [CwlContextVar("remote_chara")]
    private static Dictionary<ulong, int> SavedRemoteCharas
    {
        get => field ??= [];
        set;
    }

    /// <summary>
    ///     PeerConnect -> Prepare -> MoveZone -> SaveProbe
    /// </summary>
    public void PreparePlayerJoin(ISteamNetPeer peer)
    {
        EnsureValidation(peer);

        EmpLog.Information("Preparing player {@Peer} for joining",
            peer);

        if (SavedRemoteCharas.TryGetValue(peer.Uid, out var charaUid) &&
            game.cards.globalCharas.Find(charaUid) is { } chara) {
            // remote character exist
            SendSaveProbe(chara, peer);
        } else {
            EmpLog.Debug("Remote character does not exist, request for new character generation");

            peer.Send(new SessionNewPlayerRequest());
        }
    }

    /// <summary>
    ///     Send a save snapshot for replication
    /// </summary>
    public void SendSaveProbe(Chara chara, ISteamNetPeer peer)
    {
        EmpLog.Information("Sending save probe to player {@Peer} for replication",
            peer);

        chara.MakeAlly();
        chara.SetFlagValue("remote_chara");
        ActiveRemoteCharas[peer.Id] = chara;

        var state = States[peer.Id] = new() {
            Index = peer.Id,
            Uid = peer.Uid,
            Name = peer.Name!,
            CharaUid = chara.uid,
        };

        Session.CurrentPlayers.Add(state);

        game.Save(silent: true);

        peer.Send(SaveDataProbe.Create(chara));
    }

    /// <summary>
    ///     Net event: Client finished character generation and is ready for save probe
    /// </summary>
    private void OnSessionNewPlayerResponse(SessionNewPlayerResponse response, ISteamNetPeer peer)
    {
        EmpLog.Information("Received remote chara creation from player {@Peer}",
            peer);

        var chara = response.Chara.Decompress<Chara>();

        var remoteChara = CharaGen.Create("player");

        JsonConvert.PopulateObject(chara.ToCompactJson(), remoteChara, GameIO.jsReadGame);
        remoteChara.ChangeJob(chara.job.id);
        remoteChara.ChangeRace(chara.race.id);

        // assign a global uid
        remoteChara.SetGlobal();
        game.cards.AssignUID(remoteChara);

        SavedRemoteCharas[peer.Uid] = remoteChara.uid;

        SendSaveProbe(remoteChara, peer);
    }

    public static void RemoveRemoteChara(Chara remoteChara)
    {
        pc.party.RemoveMember(remoteChara);
        _zone.RemoveCard(remoteChara);
        if (Session.Connection is ElinNetHost host) {
            host.Delta.AddRemote(new CharaRemoveFromGameDelta {
                Owner = remoteChara,
            });
        }
    }

    [CwlPostLoad]
    private static void RemoveLeftOverCharas()
    {
        IEnumerable<Chara> excluded = Session.Connection is ElinNetHost host
            ? host.ActiveRemoteCharas.Values
            : [];

        var currentRemoteCharas = game.cards.globalCharas.Values
            .Where(c => c.GetFlagValue("remote_chara") > 0);

        foreach (var chara in currentRemoteCharas.Except(excluded)) {
            RemoveRemoteChara(chara);
        }

        pc.party?.members.RemoveAll(c => c is null);
        pc.party?.uidMembers.RemoveAll(uid => pc.party?.members.Find(c => c.uid == uid) is null);
    }
}