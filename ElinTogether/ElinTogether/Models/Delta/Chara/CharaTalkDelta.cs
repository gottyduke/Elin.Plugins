using ElinTogether.Net;
using MessagePack;
using UnityEngine;

namespace ElinTogether.Models;

[MessagePackObject]
public class CharaTalkDelta : ElinDelta
{
    [Key(0)]
    public required RemoteCard Chara;

    [Key(3)]
    public Color? Color;

    [Key(2)]
    public required float Duration;

    [Key(1)]
    public required string Text;

    protected override void OnApply(ElinNetBase net)
    {

    }
}