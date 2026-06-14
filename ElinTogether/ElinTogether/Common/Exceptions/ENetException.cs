using System;

namespace ElinTogether.Common.Exceptions;

internal class ENetException(string message) : Exception(message);