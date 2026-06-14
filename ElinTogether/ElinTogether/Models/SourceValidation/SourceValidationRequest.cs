using System.Collections.Generic;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class SourceValidationRequest
{
    [Key(0)]
    public required List<string> SourceNames { get; init; }
}