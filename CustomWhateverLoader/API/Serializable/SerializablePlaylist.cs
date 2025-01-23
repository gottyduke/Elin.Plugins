namespace Cwl.API;

public sealed record SerializablePlaylist : SerializablePlaylistV1;

public record SerializablePlaylistV1
{
    public bool Shuffle = true;
    public string[] List = [];
    public string[] Remove = [];
}