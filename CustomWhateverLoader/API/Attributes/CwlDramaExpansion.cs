using System;

namespace Cwl.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CwlDramaExpansion : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class CwlNodiscard : CwlDramaExpansion;