using System;

namespace Cwl.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
internal class CwlSourceReloadEvent : CwlEvent;