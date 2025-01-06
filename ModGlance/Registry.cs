using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BepInEx;
using Cwl.Helper;
using Cwl.Helper.Unity;
using Glance.Components;
using HarmonyLib;

namespace Glance;

internal partial class ModGlance
{
    private void RegisterGlance()
    {
        var st = new StackTrace();
        var caller = (st.GetFrames() ?? [])
            .SkipWhile(sf => {
                var ms = sf.GetMethod().Module.ScopeName;
                return ms.Contains(nameof(ModGlance)) ||
                       ms.Contains(nameof(UnityEngine));
            })
            .Select(sf => sf.GetMethod().Module.Assembly)
            .First();

        if (caller is null) {
            Log("unknown Glance register");
            return;
        }
        
        foreach (var declared in AccessTools.GetTypesFromAssembly(caller)) {
            var dispatcher = AccessTools.FirstMethod(
                declared, 
                mi => mi is { IsStatic: true, Name: "PopAll" });
            if (dispatcher?.GetParameters().Length is not 0 &&
                dispatcher?.ReturnType != typeof(IEnumerable<string>)) {
                continue;
            }

            var bep = ModManager.ListPluginObject
                .OfType<BaseUnityPlugin>()
                .FirstOrDefault(p => p.GetType().Assembly == caller);
            if (bep is null) {
                Log($"empty bepin plugin for caller {caller.FullName}");
                return;
            }
            
            gameObject.GetOrAddComponent<GlanceCollector>().AddDispatcher(new(Invocable, Info.Metadata.Name));
            Log($"registered glance for {bep.Info.Metadata}");
            break;

            IEnumerable<string> Invocable() => dispatcher.Invoke(null, []) as IEnumerable<string> ?? [];
        }
    }
}