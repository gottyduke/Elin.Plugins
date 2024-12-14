using System;
using System.Collections.Generic;
using System.Linq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Cwl.API;

public sealed class MigrateDetail
{
    private static readonly Dictionary<IWorkbook, MigrateDetail> _cached = [];

    public sealed class Row
    {
        
    }

    public static MigrateDetail GetOrAdd(IWorkbook book)
    {
        _cached.TryAdd(book, new());
        return _cached[book];
    }

    public void UpdateHeader(List<ICell> cells)
    {
        var book = _cached.FirstOrDefault(kv => kv.Value == this).Key;
        if (book is null) {
            return;
        }
        
        
    }
}