using System;

namespace Cwl.Helper.Exceptions;

public class CodeMatchException(string details) :
    InvalidOperationException(details);