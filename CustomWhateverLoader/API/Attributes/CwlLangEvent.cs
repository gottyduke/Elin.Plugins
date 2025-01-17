using System;

namespace Cwl.API;

[AttributeUsage(AttributeTargets.Method)]
public class CwlLangEvent : CwlEvent;

public class CwlLangReload : CwlLangEvent;