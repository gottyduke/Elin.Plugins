using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.Extensions;
using ElinTogether.Models;
using ElinTogether.Models.ElinDelta;
using ElinTogether.Net.Steam;
using ElinTogether.Patches;
using JetBrains.Annotations;

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
    public int SharedSpeed => GetAverageSpeed();

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

        EmpLog.Information("Preparing player {@Peer} for joining", peer);

        var chara = GetOrCreateRemoteChara(peer.Uid);
        chara.c_altName = peer.Name;

        chara.MakeAlly();
        chara.SetFlagValue("remote_chara");

        ActiveRemoteCharas[peer.Id] = chara;

        var state = States[peer.Id] = new() {
            Index = peer.Id,
            Uid = peer.Uid,
            Name = peer.Name!,
            CharaUid = chara.uid,
        };

        NetSession.Instance.CurrentPlayers.Add(state);

        SendSaveProbe(peer);
    }

    /// <summary>
    ///     Send a save snapshot for replication
    /// </summary>
    public void SendSaveProbe(ISteamNetPeer peer)
    {
        EmpLog.Information("Sending save probe to player {@Peer} for replication",
            peer);

        game.Save(silent: true);

        peer.Send(SaveDataProbe.Create(ActiveRemoteCharas[peer.Id]));
    }

    public Chara GetOrCreateRemoteChara(ulong uid)
    {
        if (SavedRemoteCharas.TryGetValue(uid, out var charaUid) &&
            game.cards.globalCharas.Find(charaUid) is { } chara) {
            return chara;
        }

        // TODO exchange player creation data with clients
        // right now we just spawn a random
        chara = CharaGen.Create("player");
        SavedRemoteCharas[uid] = chara.uid;

        return chara;
    }

    public int GetAverageSpeed()
    {
        var selfSpeed = pc.Stub_get_Speed();

        var total = selfSpeed + States.Values.Sum(s => s.Speed);
        var count = States.Count + 1;

        return total / count;
    }

    public void RemoveRemoteChara(Chara remoteChara)
    {
        pc.party.RemoveMember(remoteChara);
        _zone.RemoveCard(remoteChara);
        Delta.AddRemote(new CharaRemoveFromGameDelta {
            Owner = remoteChara,
        });
    }

    [CwlPostLoad]
    private static void RemoveLeftOverCharas()
    {
        if (NetSession.Instance.Connection is not ElinNetHost host) {
            return;
        }

        var currentRemoteCharas = game.cards.globalCharas.Values
            .Where(c => c.GetFlagValue("remote_chara") > 0);

        foreach (var chara in currentRemoteCharas.Except(host.ActiveRemoteCharas.Values)) {
            host.RemoveRemoteChara(chara);
        }
    }
}