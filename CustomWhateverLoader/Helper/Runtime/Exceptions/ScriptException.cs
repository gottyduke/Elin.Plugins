using System;

namespace Cwl.Helper.Exceptions;

public class ScriptException(string message) : Exception(message);

public class ScriptLoaderNotReadyException()
    : ScriptException("cwl_error_cs_ldr_not_ready".lang());

// TODO: add loc
public class ScriptCompilationException(string error)
    : ScriptException(error);

public class ScriptDisabledException()
    : ScriptException("cwl_error_cs_disabled".lang());