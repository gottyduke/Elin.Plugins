using System;

namespace Cwl.Helper.Runtime;

public class DramaActionArgumentException(string[] parameters) : Exception(string.Join(",", parameters));