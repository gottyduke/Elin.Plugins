﻿using System;
using System.Reflection.Emit;
using HarmonyLib;

namespace Cwl.Helper.Extensions;

public class OperandMatch(OpCode op, Func<CodeInstruction, bool> pred) : CodeMatch(o => o.opcode == op && pred(o));

public class OperandContains(OpCode op, string pattern) : OperandMatch(op, o => o.operand.ToString().Contains(pattern));

public class OpCodeContains(string pattern) :
    CodeMatch(o => o.opcode.ToString().Contains(pattern, StringComparison.InvariantCultureIgnoreCase));