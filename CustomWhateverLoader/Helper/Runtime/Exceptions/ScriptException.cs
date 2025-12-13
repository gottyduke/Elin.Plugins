using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.CodeAnalysis;

namespace Cwl.Helper.Exceptions;

public class ScriptException(string message) : Exception(message);

public class ScriptLoaderNotReadyException()
    : ScriptException("cwl_error_cs_ldr_not_ready".lang());

public class ScriptCompilationException(IEnumerable<Diagnostic> diagnostics)
    : ScriptException(diagnostics.Join(d => $"#{d.Location.SourceSpan} ({d.Id}) {d.GetMessage()}", "\n"));

public class ScriptDisabledException()
    : ScriptException("cwl_error_cs_disabled".lang());