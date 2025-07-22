namespace Cwl.API;

public sealed record SerializablePlaylist : SerializablePlaylistV1;

public record SerializablePlaylistV1
{
    public string[] List = [];
    public string[] Remove = [];
    public bool Shuffle = true;
}