using System;

namespace Cwl.API;

[AttributeUsage(AttributeTargets.Method)]
public class CwlGameIOEvent : CwlEvent;

public class CwlPreLoad : CwlGameIOEvent;

public class CwlPostLoad : CwlGameIOEvent;

public class CwlPreSave : CwlGameIOEvent;

public class CwlPostSave : CwlGameIOEvent;