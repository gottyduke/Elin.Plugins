using System;

namespace EModding.Helper.Runtime.Exceptions;

public class CodeMatchException(string details) :
    InvalidOperationException(details);