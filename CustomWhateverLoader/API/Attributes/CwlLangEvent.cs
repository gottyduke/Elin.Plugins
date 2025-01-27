using System;

namespace Cwl.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CwlLangEvent : CwlEvent;

public class CwlLangReload : CwlLangEvent;