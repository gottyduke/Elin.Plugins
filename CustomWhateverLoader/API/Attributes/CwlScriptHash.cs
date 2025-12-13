using System;

namespace Cwl.API.Attributes;

[AttributeUsage(AttributeTargets.Assembly)]
public class CwlScriptHash(string hash) : Attribute
{
    public string Hash => hash;
}