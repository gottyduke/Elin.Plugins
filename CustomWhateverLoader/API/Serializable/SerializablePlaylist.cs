using System.Collections.Generic;

namespace Cwl.API;

public sealed record SerializablePlaylist : SerializablePlaylistV1;

// ReSharper disable all
public record SerializablePlaylistV1
{
    public List<string> List = [];
    public List<string> Remove = [];
    public bool Shuffle = true;
}