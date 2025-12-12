using System.Collections.Generic;
using Cwl.Scripting;
using ReflexCLI.Attributes;

namespace Cwl.API.Drama;

[ConsoleCommandClassCustomizer("cwl.dm")]
public partial class DramaExpansion : DramaOutcome
{
    public static ActionCookie? Cookie { get; internal set; }

    public static ExcelData? CurrentData =>
        DramaManager.dictCache.GetValueOrDefault($"{CorePath.DramaData}{Cookie?.Dm.setup.book}.xlsx");

    public static void ResetStates()
    {
        _valueStack.Clear();
        CwlScriptLoader.ClearState("drama");
    }
}