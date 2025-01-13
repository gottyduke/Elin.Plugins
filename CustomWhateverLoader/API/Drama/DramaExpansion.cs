using System;
using System.Collections.Generic;
using Cwl.Helper.Runtime;

namespace Cwl.API.Drama;

public partial class DramaExpansion
{
    public delegate bool DramaAction(DramaManager dm, Dictionary<string, string> item, params string[] parameters);

    public static bool emit_call(DramaManager dm, Dictionary<string, string> item, params string[] parameters)
    {
        if (parameters is not [{ } methodName, .. { } pack]) {
            return false;
        }
        
        if (methodName.StartsWith("ext.")) {
            BuildActionList(true);
            methodName = methodName[4..];
        }
        
        if (!Cached.TryGetValue(methodName, out var action)) {
            return false;
        }
 
        if (pack.Length == action.ParameterCount) {
            CwlMod.Debug<DramaExpansion>($"call [{methodName}, {pack.Length}]");
            return SafeInvoke(action, dm, item, pack);
        }

        CwlMod.Warn<DramaExpansion>($"failed emitting call [{methodName}], {action.ParameterCount}\n" +
                                    string.Join(", ", parameters));
        return false;
    }
    
    public static bool check_affinity(DramaManager dm, Dictionary<string, string> item, params string[] parameters)
    {
        return parameters is [{ } id, { } expr] && 
               game.cards.globalCharas.Find(id.Trim()) is { } chara &&
               Compare(chara._affinity, expr);
    }

    public static bool debug_invoke(DramaManager dm, Dictionary<string, string> item, params string[] parameters)
    {
        pc.Say($"debug_invoke : {dm.tg.Name}");
        return true;
    }
}