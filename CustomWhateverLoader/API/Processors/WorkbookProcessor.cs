using System;
using Cwl.LangMod;
using Cwl.Loader;
using NPOI.SS.UserModel;

namespace Cwl.API.Processors;

public class WorkbookProcessor
{
    public delegate void WorkbookProcess(IWorkbook workbook);

    private static event WorkbookProcess OnWorkbookPostProcess = delegate { };
    private static event WorkbookProcess OnWorkbookPreProcess = delegate { };

    public static void Add(WorkbookProcess bookProcess, bool post)
    {
        if (post) {
            OnWorkbookPostProcess += Process;
        } else {
            OnWorkbookPreProcess += Process;
        }

        return;

        void Process(IWorkbook book)
        {
            try {
                bookProcess(book);
            } catch (Exception ex) {
                var type = post ? "post" : "pre";
                CwlMod.Warn("cwl_warn_processor".Loc("book", type, ex.Message));
                // noexcept
            }
        }
    }

    internal static void PreProcess(IWorkbook workbook)
    {
        if (CwlConfig.AllowProcessors) {
            OnWorkbookPreProcess(workbook);
        }
    }

    internal static void PostProcess(IWorkbook workbook)
    {
        if (CwlConfig.AllowProcessors) {
            OnWorkbookPostProcess(workbook);
        }
    }
}