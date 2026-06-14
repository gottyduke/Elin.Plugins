using MessagePack;
using Newtonsoft.Json;

namespace ElinTogether.Models;

/// <summary>
///     Net packet: Host -> Client
/// </summary>
[MessagePackObject]
public class SaveDataProbe
{
    private static readonly JsonSerializer _serializer = JsonSerializer.Create(GameIO.jsReadGame);

    [Key(0)]
    public required LZ4Bytes Game { get; init; }

    [Key(1)]
    public required int RemoteCharaUid { get; init; }

    public Game MakeGameSave()
    {
        return Game.Decompress<Game>(_serializer);
    }

    public static SaveDataProbe Create(int uid)
    {
        return new() {
            Game = LZ4Bytes.Create(EClass.game, _serializer),
            RemoteCharaUid = uid,
        };
    }
}