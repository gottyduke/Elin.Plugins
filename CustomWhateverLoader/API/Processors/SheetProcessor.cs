using System;
using Cwl.LangMod;
using Cwl.Loader;
using NPOI.SS.UserModel;

namespace Cwl.API.Processors;

public class SheetProcessor
{
    public delegate void SheetProcess(ISheet sheet);

    private static event SheetProcess OnSheetPostProcess = _ => { };
    private static event SheetProcess OnSheetPreProcess = _ => { };

    public static void Add(SheetProcess sheetProcess, bool post = true)
    {
        var error = post ? "cwl_book_post_process" : "cwl_book_pre_process";

        if (post) {
            OnSheetPostProcess += Process;
        } else {
            OnSheetPreProcess += Process;
        }

        return;

        void Process(ISheet book)
        {
            try {
                sheetProcess(book);
            } catch (Exception ex) {
                CwlMod.Warn(error.Loc(ex));
                // noexcept
            }
        }
    }

    internal static void PreProcess(ISheet sheet)
    {
        if (CwlConfig.AllowProcessors) {
            OnSheetPreProcess(sheet);
        }
    }

    internal static void PostProcess(ISheet sheet)
    {
        if (CwlConfig.AllowProcessors) {
            OnSheetPostProcess(sheet);
        }
    }
}