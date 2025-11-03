using System;

namespace Cwl.API.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class CwlContextVar(string chunkName) : CwlEvent
{
    public string ChunkName => chunkName;
}