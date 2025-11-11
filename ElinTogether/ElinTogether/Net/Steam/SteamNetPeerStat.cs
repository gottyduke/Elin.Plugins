using System;
using Cwl.Helper.String;

namespace ElinTogether.Net.Steam;

public sealed class SteamNetPeerStat
{
    public float AvgBpsIn;

    public float AvgBpsOut;
    public float AvgPingMs;
    public long BytesReceived;

    public long BytesSent;
    public float ConnectionQualityLocal;
    public float ConnectionQualityRemote;

    public int LastPingMs;

    public DateTime LastUpdated;
    public int PacketsReceived;
    public int PacketsSent;

    public SteamNetPeerStat Clone()
    {
        return (SteamNetPeerStat)MemberwiseClone();
    }

    public string ToStringSimplified()
    {
        return $"Ping={AvgPingMs:F1}ms\tOut={AvgBpsOut / 1024f:F1} KB/s\tIn={AvgBpsIn / 1024f:F1} KB/s";
    }

    public override string ToString()
    {
        return $"Ping={AvgPingMs:F1}ms\tQoS L={ConnectionQualityLocal:P0}, R={ConnectionQualityRemote:P0}\n" +
               $"Out={AvgBpsOut / 1024f:F1} KB/s\tIn={AvgBpsIn / 1024f:F1} KB/s\n" +
               $"Sent={BytesSent.ToAllocateString()}\tRecv={BytesReceived.ToAllocateString()}";
    }

    public void Received(int bytes)
    {
        BytesReceived += bytes;
        PacketsReceived++;
    }

    public void Sent(int bytes)
    {
        BytesSent += bytes;
        PacketsSent++;
    }
}