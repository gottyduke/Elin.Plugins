using System.Collections.Generic;
using ElinTogether.Helper;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class SourceValidationResponse
{
    [Key(0)]
    public required Dictionary<string, string> Checksums { get; init; }

    [Key(1)]
    public required List<string> Assemblies { get; init; }

    public static SourceValidationResponse Create(IEnumerable<string> sourceNames)
    {
        return new() {
            Checksums = SourceValidation.GenerateAll(sourceNames),
            Assemblies = SourceValidation.GetSourceAssemblies(),
        };
    }

    public static SourceValidationResponse Create(Dictionary<string, string> checksums)
    {
        return new() {
            Checksums = checksums,
            Assemblies = SourceValidation.GetSourceAssemblies(),
        };
    }
}