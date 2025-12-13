using System;

namespace Cwl.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CwlDramaExpansion : CwlEvent;

[AttributeUsage(AttributeTargets.Method)]
public class CwlNodiscard : CwlDramaExpansion;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CwlDramaAction(string action) : CwlDramaExpansion
{
    public string Action => action;
}