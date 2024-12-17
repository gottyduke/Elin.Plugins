using System;
using Cwl.LangMod;
using Cwl.Loader;
using NPOI.SS.UserModel;

namespace Cwl.API.Processors;

public class WorkbookProcessor
{
    public delegate void WorkbookProcess(IWorkbook workbook);

    private static event WorkbookProcess OnWorkbookPostProcess = _ => { };
    private static event WorkbookProcess OnWorkbookPreProcess = _ => { };

    public static void Add(WorkbookProcess bookProcess, bool post = true)
    {
        var error = post ? "cwl_book_post_process" : "cwl_book_pre_process";

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
                CwlMod.Warn(error.Loc(ex));
                // noexcept
            }
        }
    }

    internal static void PreProcess(IWorkbook workbook)
    {
        OnWorkbookPreProcess(workbook);
    }

    internal static void PostProcess(IWorkbook workbook)
    {
        OnWorkbookPostProcess(workbook);
    }
}