#if !DEBUG
global using SwallowExceptions.Fody;

#else
using System;

namespace Emmersive;

internal class SwallowExceptions : Attribute;
#endif