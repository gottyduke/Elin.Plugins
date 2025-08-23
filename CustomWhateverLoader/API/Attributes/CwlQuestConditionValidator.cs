using System;

namespace Cwl.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CwlQuestConditionValidator : CwlEvent;