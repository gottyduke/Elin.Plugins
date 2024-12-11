using System;

namespace Cwl.API;

public sealed class SourceParseException(string detail, Exception innerException) : Exception(detail, innerException);