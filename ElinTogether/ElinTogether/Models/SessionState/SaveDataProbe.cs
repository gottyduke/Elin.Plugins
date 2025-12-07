using MessagePack;

namespace ElinTogether.Models;

/// <summary>
///     Net packet: Host -> Client
/// </summary>
[MessagePackObject]
public class SaveDataProbe
{
    [Key(0)]
    public required LZ4Bytes Game { get; init; }

    [Key(1)]
    public required LZ4Bytes Chara { get; init; }

    public static SaveDataProbe Create(Chara chara)
    {
        return new() {
            Game = LZ4Bytes.Create(EClass.game),
            Chara = LZ4Bytes.Create(chara),
        };
    }
}