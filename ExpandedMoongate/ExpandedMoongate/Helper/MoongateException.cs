using System;

namespace Exm.Helper;

public class MoongateException(string message) : Exception(message);