using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Cwl.Helper.Exceptions;
using HarmonyLib;

namespace Cwl.Helper.Extensions;

public class OperandMatch(OpCode op, Func<CodeInstruction, bool> pred) :
    CodeMatch(o => o.opcode == op && pred(o));

public class OperandContains(OpCode op, string pattern) :
    OperandMatch(op, o => o.operand.ToString().Contains(pattern, StringComparison.InvariantCultureIgnoreCase));

public class OpCodeContains(string pattern) :
    CodeMatch(o => o.opcode.ToString().Contains(pattern, StringComparison.InvariantCultureIgnoreCase));

public static class CodeMatchExt
{
    extension(CodeMatcher cm)
    {
        public CodeMatcher EnsureValid(string details, [CallerMemberName] string patcher = "")
        {
            return cm.IsInvalid
                ? throw new CodeMatchException($"failed to match {patcher} {details} / {cm.Pos}")
                : cm;
        }
    }
}