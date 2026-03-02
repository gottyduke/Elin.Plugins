using System;

namespace EGate.Helper;

public class MoongateException(string message) : Exception(message);