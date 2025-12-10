using System;
using Cwl.Helper.String;

namespace Cwl.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CwlContextMenu(string entryOrLangId, string displayNameOrLangId = "") : CwlEvent
{
    public string BtnName => displayNameOrLangId.EmptyOr(Entry.Split('/')[^1]).lang();

    public string Entry => entryOrLangId.lang();
}