#if !DEBUG
global using SwallowExceptions.Fody;

#else
using System;

namespace ElinPad;

internal class SwallowExceptions : Attribute;
#endif