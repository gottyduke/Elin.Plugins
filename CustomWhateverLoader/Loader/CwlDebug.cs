#if !DEBUG
global using SwallowExceptions.Fody;

#else
using System;

namespace Cwl;

internal class SwallowExceptions : Attribute;
#endif