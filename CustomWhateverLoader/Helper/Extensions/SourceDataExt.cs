using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cwl.API;

namespace Cwl.Helper.Extensions;

public static class SourceDataExt
{
    extension(SourceData source)
    {
        public int ImportRows(IEnumerable<SourceData.BaseRow>? rows)
        {
            if (rows is null) {
                return 0;
            }

            if (source.GetFieldValue("rows") is not IList list) {
                return 0;
            }

            var rowsImported = 0;
            foreach (var row in rows) {
                row.OnImportData(source);
                list.Add(row);
                rowsImported++;
            }

            var sourceType = source.GetType();
            sourceType.GetRuntimeMethod("OnAfterImportData", []).FastInvoke(source);

            CwlMod.CurrentLoading = $"{sourceType.Name}/{rowsImported}";
            CwlMod.Log<WorkbookImporter>(CwlMod.CurrentLoading);

            return rowsImported;
        }
    }
}