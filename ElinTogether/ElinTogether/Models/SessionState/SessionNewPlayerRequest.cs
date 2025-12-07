using MessagePack;

namespace ElinTogether.Models;

/// <summary>
///     Net packet: Host -> Client
/// </summary>
[MessagePackObject]
public class SessionNewPlayerRequest
{
    public SessionNewPlayerResponse Ready()
    {
        return new() {
            Chara = LZ4Bytes.Create(EClass.pc),
        };
    }
}