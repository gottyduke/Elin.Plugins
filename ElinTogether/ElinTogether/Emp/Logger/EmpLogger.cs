global using EmpLog = Serilog.Log;
global using EmpPop = ElinTogether.EmpLogger;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using Cwl.Helper.String;
using ElinTogether.Helper;
using ElinTogether.Models;
using ElinTogether.Net;
using ElinTogether.Net.Steam;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using UnityEngine;
using ILogger = Serilog.ILogger;

#pragma warning disable CA2254

namespace ElinTogether;

internal static partial class EmpLogger
{
    private static readonly ConcurrentDictionary<IPAddress, string> _hashCache = [];

    
    private static ILogger DefaultLogger => field ??= GetDefaultLoggerConfiguration().CreateLogger();

    internal static LoggerConfiguration GetDefaultLoggerConfiguration()
    {
        return new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Verbose()
#else
            .MinimumLevel.Debug()
#endif
            .ConfigureDestructures()
            .ConfigureEnrichers()
            .ConfigureSinks();
    }

    internal static void InitLogger(ILogger? custom = null)
    {
        EmpLog.Logger = custom ?? DefaultLogger;
    }

    extension(LoggerConfiguration lc)
    {
        private LoggerConfiguration ConfigureEnrichers()
        {
            return lc
                .Enrich.FromLogContext()
                .Enrich.With<NetSessionStateEnricher>()
                .Enrich.When(
                    l => l.Level >= LogEventLevel.Warning,
                    lec => lec.WithProperty("EmpVersion", ModInfo.BuildVersion));
        }

        private LoggerConfiguration ConfigureDestructures()
        {
            return lc
                .Destructure.ByTransforming<SteamNetPeer>(p => new {
                    Id = p.Colorize(p.Id),
                    Name = p.Colorize(p.Name),
                })
                .Destructure.ByTransforming<SteamNetPeerStat>(ps => new {
                    Sent = ps.BytesSent.ToAllocateString(),
                    Received = ps.BytesReceived.ToAllocateString(),
                    ps.PacketsSent,
                    ps.PacketsReceived,
                    ps.LastPingMs,
                    AvgPingMs = Math.Round(ps.AvgPingMs, 1),
                    ps.ConnectionQualityLocal,
                    ps.ConnectionQualityRemote,
                    OutKBps = Math.Round(ps.AvgBpsOut / 1024f, 1),
                    InKBps = Math.Round(ps.AvgBpsIn / 1024f, 1),
                    LastUpdated = ps.LastUpdated.ToString("HH:mm:ss"),
                })
                .Destructure.ByTransforming<NetPeerState>(ps => new {
                    ps.Index,
                    ps.Uid,
                    ps.Name,
                })
                .Destructure.ByTransforming<NetSession>(s => new {
                    Type = s.IsHost ? "Host" : "Client",
                    Id = s.SessionId,
                    s.Tick,
                    s.SyncMode,
                })
                .Destructure.ByTransforming<Point>(p => new {
                    X = p.x,
                    Z = p.z,
                })
                .Destructure.ByTransforming<MapDataRequest>(z => new {
                    ZoneFullName = z.ZoneFullName.TagColor(0x009e73),
                    z.ZoneUid,
                })
                .Destructure.ByTransforming<ZoneDataResponse>(z => new {
                    ZoneFullName = z.ZoneFullName.TagColor(0x009e73),
                    z.ZoneUid,
                })
                .Destructure.ByTransforming<RemoteCard>(p => new {
                    p.Uid,
                });
        }

        private LoggerConfiguration ConfigureSinks()
        {
            return lc
                .WriteTo.Console(
                    outputTemplate: "[EMP][{Level:u4}-{Timestamp:HH:mm:ss}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    new CompactJsonFormatter(new PlainJsonValueFormatter()),
                    Path.Combine(Application.persistentDataPath, "ElinMP/Logs/Session_.log"),
                    LogEventLevel.Debug,
#if DEBUG
                    shared: true,
#else
                    buffered: true,
#endif
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 3);
        }
    }

    extension(IPAddress address)
    {
        internal string RedactedIp =>
            _hashCache.GetOrAdd(address,
                ip => Convert.ToBase64String(ip.ToString().GetSha256Hash().ToArray(), 0, 6)
                    .TrimEnd('=')
                    .Replace('+', '-')
                    .Replace('/', '_'));
    }

    extension(IPEndPoint endPoint)
    {
        internal string RedactedIp => endPoint.Address.RedactedIp;
    }

    extension(string address)
    {
        internal string RedactedIp =>
            IPAddress.TryParse(address, out var ipv4Or6) ||
            IPAddress.TryParse(address.Split(':')[0], out ipv4Or6)
                ? ipv4Or6.RedactedIp
                : address;
    }
}