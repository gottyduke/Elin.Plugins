using System;
using Cwl.LangMod;
using NPOI.SS.UserModel;

namespace Cwl.API.Processors;

public class SheetProcessor
{
    public delegate void SheetProcess(ISheet sheet);

    private static event SheetProcess OnSheetPostProcess = delegate { };
    private static event SheetProcess OnSheetPreProcess = delegate { };

    public static void Add(SheetProcess sheetProcess, bool post)
    {
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
                var type = post ? "post" : "pre";
                CwlMod.Warn<SheetProcessor>("cwl_warn_processor".Loc("sheet", type, ex.Message));
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