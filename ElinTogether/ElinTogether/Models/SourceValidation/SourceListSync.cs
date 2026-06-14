using System.Collections;
using System.Collections.Generic;
using MessagePack;

namespace ElinTogether.Models;

[MessagePackObject]
public class SourceListSync
{
    [Key(0)]
    public required Dictionary<string, LZ4Bytes> SourceRows { get; init; }

    public void Apply()
    {
        foreach (var (sourceType, lz4) in SourceRows) {
            var rows = lz4.Decompress<SourceData.BaseRow[]>();
            var source = ModUtil.FindSourceByName(sourceType);
            source.Reset();
            source.GetField<IList>("rows").Clear();
            source.ImportRows(rows);
            source.Init();
            EmpLog.Debug("Applied source sync for {SourceData}, {SourceDataCount} rows, ({SourceDataSize})",
                sourceType, rows.Length, lz4.Bytes.Length.ToAllocateString());
        }

        // source cards
        EMono.sources.cards.Init();
    }
}