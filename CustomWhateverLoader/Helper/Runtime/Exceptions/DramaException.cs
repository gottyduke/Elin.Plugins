using System;
using HarmonyLib;

namespace Cwl.Helper.Runtime.Exceptions;

public class DramaActionArgumentException(int count, string[] parameters) : 
    Exception($"expected {count}, got [{parameters.Join()}");

public class DramaActionInvokeException(string callName) : Exception(callName);