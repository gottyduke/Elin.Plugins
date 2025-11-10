using System;
using ElinTogether.Helper;
using MessagePack;

namespace ElinTogether.Net.Steam;

internal class SteamNetSerializer : ISteamNetSerializer
{
    private const int HeaderSize = sizeof(uint);

    public byte[] Serialize<T>(T obj)
    {
        var typeHash = SteamNetTypeRegistry.GetHash<T>();
        var payload = MessagePackSerializer.Serialize(obj);

        var bytes = new byte[HeaderSize + payload.Length];

        FastBitConverter.GetBytes(bytes, 0, typeHash);
        Buffer.BlockCopy(payload, 0, bytes, HeaderSize, payload.Length);

        return bytes;
    }

    public object Deserialize(byte[] data, Type type)
    {
        return MessagePackSerializer.Deserialize(type, data)!;
    }

    public static (uint typeHash, byte[] payload) ExtractTypeAndPayload(byte[] data)
    {
        var typeHash = BitConverter.ToUInt32(data, 0);
        var payload = new byte[data.Length - HeaderSize];
        Buffer.BlockCopy(data, HeaderSize, payload, 0, payload.Length);

        return (typeHash, payload);
    }
}