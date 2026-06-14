using ElinTogether.Models;
using HarmonyLib;

namespace ElinTogether.Net;

internal partial class ElinNetClient
{
    /// <summary>
    ///     Net event: Host requested source checksums. Compute locally
    /// </summary>
    private void OnSourceValidationRequest(SourceValidationRequest request)
    {
        EmpLog.Debug("Received source validation request for {Count} sources",
            request.SourceNames.Count);

        Host.Send(SourceValidationResponse.Create(SourceList));
    }

    /// <summary>
    ///     Net event: Host sent source rows to sync
    /// </summary>
    private void OnSourceListSync(SourceListSync sync)
    {
        EmpLog.Information("Received source sync for {Count} sources, [{SourceSyncs}]",
            sync.SourceRows.Count, sync.SourceRows.Keys.Join());

        sync.Apply();

        CreateValidation();

        Host.Send(SourceValidationResponse.Create(SourceList));

        EmpLog.Debug("Re-sent source validation after applying sync");
    }
}