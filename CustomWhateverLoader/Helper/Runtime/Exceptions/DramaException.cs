using System;

namespace Cwl.Helper.Runtime.Exceptions;

public class DramaActionArgumentException(string[] parameters) : Exception(string.Join(",", parameters));

public class DramaActionInvokeException(string callName) : Exception(callName);