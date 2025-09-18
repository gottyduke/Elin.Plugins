using System;

namespace Cwl.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CwlContextMenu(string entryOrLangId, string displayNameOrIdLang = "") : CwlEvent
{
    public string BtnName => displayNameOrIdLang.IsEmpty(entryOrLangId.Split("/")[^1]).lang();

    public string Entry => entryOrLangId.lang();
}