using System;

namespace Cwl.API.Attributes;

public class CwlOnCreateEvent : CwlEvent;

[AttributeUsage(AttributeTargets.Method)]
public class CwlCharaOnCreateEvent : CwlOnCreateEvent;

[AttributeUsage(AttributeTargets.Method)]
public class CwlThingOnCreateEvent : CwlOnCreateEvent;