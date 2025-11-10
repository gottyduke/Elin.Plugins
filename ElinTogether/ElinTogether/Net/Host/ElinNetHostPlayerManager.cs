using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.Helper.Extensions;
using ElinTogether.Models;
using ElinTogether.Net.Steam;

namespace ElinTogether.Net;

internal partial class ElinNetHost
{
    public readonly HashSet<Chara> ActiveRemoteCharas = [];

    [CwlContextVar("remote_chara")]
    [field: AllowNull]
    private static ConcurrentDictionary<ulong, int> SavedRemoteCharas
    {
        get => field ??= [];
        set {
            if (value.Count > 0) {
                EmpLog.Debug("Restoring {RemoteCharaCount} remote characters",
                    value.Count);
            }
            field = value;
        }
    }

    /// <summary>
    ///     PeerConnect -> Prepare -> SaveProbe
    /// </summary>
    public void PreparePlayerJoin(ISteamNetPeer peer)
    {
        EmpLog.Information("Preparing player {@Peer} for joining", peer);

        var chara = GetOrCreateRemoteChara(peer.Uid);
        chara.c_altName = peer.Name;

        States[peer.Id] = new() {
            Index = peer.Id,
            Uid = peer.Uid,
            Name = peer.Name,
            IsValidated = true,
            Chara = chara,
        };

        SendSaveProbe(peer);
    }

    public void SendSaveProbe(ISteamNetPeer peer)
    {
        EmpLog.Information("Sending save probe to player {@Peer} for replication",
            peer);

        game.Save(false, true);

        peer.Send(SaveDataProbe.Create(States[peer.Id].Chara));
    }

    public Chara? GetRemoteCharaFromPeer(ISteamNetPeer peer)
    {
        return States.TryGetValue(peer.Id, out var state)
            ? state.Chara
            : null;
    }

    public Chara GetOrCreateRemoteChara(ulong uid)
    {
        if (!SavedRemoteCharas.TryGetValue(uid, out var charaUid) ||
            game.cards.globalCharas.Find(charaUid) is not { } chara) {
            chara = CharaGen.Create("player");
            SavedRemoteCharas[uid] = chara.uid;
        }

        chara.MakeAlly();
        chara.SetFlagValue("remote_chara");

        return chara;
    }

    public static void RemoveRemoteChara(Chara remoteChara)
    {
        pc.party.RemoveMember(remoteChara);
        _zone.RemoveCard(remoteChara);
    }

    [CwlPostLoad]
    private static void CleanUpLeftOverCharas()
    {
        if (NetSession.Instance.Connection is not ElinNetHost { ActiveRemoteCharas: { } active }) {
            active = [];
        }

        var currentRemoteCharas = game.cards.globalCharas.Values
            .Where(c => c.GetFlagValue("remote_chara") > 0);

        foreach (var chara in currentRemoteCharas.Except(active)) {
            RemoveRemoteChara(chara);
        }
    }
}