using System;

namespace Cwl.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CwlCharaOnCreateEvent : CwlEvent;