using System.Collections.Generic;
using System.Linq;
using ElinTogether.Helper;
using ElinTogether.Models;

namespace ElinTogether.Net;

public partial class ElinNetBase
{
    /// <summary>
    ///     Cached source table checksums for this session.
    ///     Host populates this in <c>CreateValidation</c> (called during Initialize).
    ///     Client receives authoritative list via <see cref="SourceValidationRequest" />.
    /// </summary>
    protected Dictionary<string, string> SourceList { get; private set; } = [];

    /// <summary>
    ///     Called once during <see cref="Initialize" /> to build the host's authoritative
    ///     source checksum list. Client also calls this to have a baseline (though it will
    ///     be overwritten by the host's request during validation phase).
    /// </summary>
    public void CreateValidation()
    {
        var oldList = SourceList;
        SourceList = SourceValidation.GenerateAll(SourceValidation.DefaultSources);

        EmpLog.Information("Created source validation rules for {Count} sources",
            SourceList.Count);

        foreach (var (sourceName, sha) in SourceList) {
            var oldSha = oldList.GetValueOrDefault(sourceName);
            var newSha = (oldSha == sha && oldSha is not null) ? "unchanged" : sha;
            EmpLog.Debug("{SourceData,-16}[{OldSourceDataSha}] -> [{NewSourceDataSha}]",
                sourceName, oldSha, newSha);
        }
    }

    /// <summary>
    ///     Returns the list of source names the host expects clients to validate.
    ///     Used when constructing <see cref="SourceValidationRequest" />.
    /// </summary>
    protected List<string> GetValidationSourceNames()
    {
        // TODO: read from server rule
        return SourceList.Keys.ToList();
    }
}