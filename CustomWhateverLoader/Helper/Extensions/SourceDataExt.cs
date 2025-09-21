using System.Collections;
using System.Reflection;

namespace Cwl.Helper.Extensions;

public static class SourceDataExt
{
    extension(SourceData source)
    {
        public int ImportRows(SourceData.BaseRow[] rows)
        {
            if (source.GetFieldValue("rows") is not IList list) {
                return 0;
            }

            foreach (var row in rows) {
                row.OnImportData(source);
                list.Add(row);
            }

            var sourceType = source.GetType();
            sourceType.GetRuntimeMethod("OnAfterImportData", []).FastInvoke(source);

            return rows.Length;
        }
    }
}