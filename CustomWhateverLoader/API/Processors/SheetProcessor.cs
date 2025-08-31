using System;
using Cwl.LangMod;
using NPOI.SS.UserModel;

namespace Cwl.API.Processors;

public class SheetProcessor
{
    private static event Action<ISheet>? OnSheetPostProcess;
    private static event Action<ISheet>? OnSheetPreProcess;

    public static void Add(Action<ISheet> sheetProcess, bool post)
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
                CwlMod.Warn<SheetProcessor>("cwl_warn_processor".Loc("sheet", type, ex));
                // noexcept
            }
        }
    }

    internal static void PreProcess(ISheet sheet)
    {
        if (CwlConfig.AllowProcessors) {
            OnSheetPreProcess?.Invoke(sheet);
        }
    }

    internal static void PostProcess(ISheet sheet)
    {
        if (CwlConfig.AllowProcessors) {
            OnSheetPostProcess?.Invoke(sheet);
        }
    }
}