using System;

namespace EModding.Helper.Runtime.Exceptions;

public sealed class SourceParseException(string detail, Exception? innerException = null)
    : Exception(detail, innerException ?? new());