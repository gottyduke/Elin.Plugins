using System;

namespace Cwl.Helper.Exceptions;

public class BeggarException(string id) : Exception($"character {id} turned into a beggar");