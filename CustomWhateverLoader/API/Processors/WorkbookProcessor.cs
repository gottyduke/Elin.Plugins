using System;
using Cwl.LangMod;
using NPOI.SS.UserModel;

namespace Cwl.API.Processors;

public class WorkbookProcessor
{
    private static event Action<IWorkbook>? OnWorkbookPostProcess;
    private static event Action<IWorkbook>? OnWorkbookPreProcess;

    public static void Add(Action<IWorkbook> bookProcess, bool post)
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
                CwlMod.Warn<WorkbookProcessor>("cwl_warn_processor".Loc("book", type, ex));
                // noexcept
            }
        }
    }

    internal static void PreProcess(IWorkbook workbook)
    {
        if (CwlConfig.AllowProcessors) {
            OnWorkbookPreProcess?.Invoke(workbook);
        }
    }

    internal static void PostProcess(IWorkbook workbook)
    {
        if (CwlConfig.AllowProcessors) {
            OnWorkbookPostProcess?.Invoke(workbook);
        }
    }
}