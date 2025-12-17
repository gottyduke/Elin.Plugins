using System.Collections.Generic;
using Cwl.Scripting;

// ReSharper disable InconsistentNaming

namespace Cwl.API.Drama;

public class DramaScriptState : CwlScriptLoader.CwlScriptState
{
    public required DramaManager dm;
    public required Dictionary<string, string> line;
    public Chara pc => EClass.pc;
    public Chara tg => dm.GetChara("tg");

    public string text
    {
        get => line["text"];
        set => line["text"] = value;
    }
}