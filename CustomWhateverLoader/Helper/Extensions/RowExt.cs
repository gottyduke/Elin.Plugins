using System.Collections.Generic;
using System.Linq;

namespace Cwl.Helper.Extensions;

public static class RowExt
{
    internal static readonly Dictionary<ModPackage, HashSet<SourceData.BaseRow>> Rows = [];

    extension(SourceData.BaseRow row)
    {
        public ModPackage? WhereTheF => Rows.FirstOrDefault(kv => kv.Value.Contains(row)).Key;
    }
}