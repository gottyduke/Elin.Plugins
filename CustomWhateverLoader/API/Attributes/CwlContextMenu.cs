using System;

namespace Cwl.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CwlContextMenu(string entryOrLangId, string displayNameOrLangId = "") : CwlEvent
{
    public string BtnName => displayNameOrLangId.IsEmpty(Entry.Split('/')[^1]).lang();

    public string Entry => entryOrLangId.lang();
}