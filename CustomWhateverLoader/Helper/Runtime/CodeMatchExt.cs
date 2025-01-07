using System;
using System.Reflection.Emit;
using HarmonyLib;

namespace Cwl.Helper.Runtime;

public class OperandMatch(OpCode op, Func<CodeInstruction, bool> pred) : CodeMatch(o => o.opcode == op && pred(o));

public class OperandContains(OpCode op, string pattern) : OperandMatch(op, o => o.operand.ToString().Contains(pattern));