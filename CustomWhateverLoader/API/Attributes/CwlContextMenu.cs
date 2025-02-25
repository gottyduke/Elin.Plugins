using System;

namespace Cwl.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CwlContextMenu(string entry, string idLang = "") : CwlEvent
{
    public string BtnName => idLang.IsEmpty(entry.Split("/")[^1]).lang();

    public string Entry => entry;
}