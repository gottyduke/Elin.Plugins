using System;

namespace Cwl.Helper.Exceptions;

public sealed class SourceParseException(string detail, Exception? innerException = null)
    : Exception(detail, innerException ?? new());