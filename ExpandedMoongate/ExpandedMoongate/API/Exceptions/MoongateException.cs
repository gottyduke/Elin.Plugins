using System;

namespace Exm.API.Exceptions;

public class MoongateException(string message) : Exception(message);