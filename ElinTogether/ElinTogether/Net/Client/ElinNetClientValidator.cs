using System;
using System.Linq;
using Cwl.Helper.String;
using ElinTogether.Helper;
using ElinTogether.Models;
using Serilog.Events;

namespace ElinTogether.Net;

internal partial class ElinNetClient
{
    /// <summary>
    ///     Net event: Source validation requested, sending checksums
    /// </summary>
    private void OnSourceListRequest(SourceListRequest request)
    {
        SourceValidation.ThrowIfInvalid(request.Type);

        if (request.Type == SourceListType.All) {
            foreach (var source in SourceList.Keys) {
                SendSourceList(source);
            }
        } else {
            SendSourceList(request.Type);
        }

        return;

        void SendSourceList(SourceListType sourceType)
        {
            Socket.FirstPeer.Send(new SourceListResponse {
                Type = sourceType,
                Checksum = SourceList[sourceType],
            });

            EmpLog.Information("Sending source list validation {SourceListType} to host",
                sourceType);
        }
    }

    /// <summary>
    ///     Net event: Source validation failed
    /// </summary>
    private void OnSourceDiffResponse(SourceDiffResponse response)
    {
        SourceValidation.ThrowIfInvalid(response.Type);

        var current = SourceValidation
            .GenerateSourceIdList(response.Type)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        var received = response.IdList
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        using var sb = StringBuilderPool.Get();

        for (var i = 0; i < Math.Max(received.Length, current.Length); ++i) {
            var idHost = received.TryGet(i, true);
            var idSelf = current.TryGet(i, true);

            if (idHost != idSelf) {
                sb.AppendLine($"row {i}, host:{idHost}, self:{idSelf}");
            }
        }

        EmpPop.Popup(LogEventLevel.Warning, "Failed to validate source list {SourceListType}\n{SourceListDiff}",
            response.Type, sb.ToString());

        Socket.Disconnect(Socket.FirstPeer, "emp_source_invalid");
    }
}