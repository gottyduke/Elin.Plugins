using EModding.Components;
using EModding.Helper.Runtime;
using ReflexCLI;
using ReflexCLI.UI;
using UnityEngine;

namespace EModding;

internal partial class EModdingKit
{
    private void Start()
    {
        EScript.SetProvider(new EScriptProvider());
        EScriptLoader.LoadAllPackageScripts();
        InitConsole();
    }

    private void OnStartCore()
    {
        SetupExceptionHook();
        TypeQualifier.SafeQueryTypesOfAll();
        ModIntegrity.SetupEvent();
    }

    private static void InitConsole()
    {
        CommandRegistry.Init();
        ParameterProcessorRegistry.Init();

        var console = ReflexConsole.Instance;
        console.ui = Instantiate(Resources.Load<ReflexUIManager>("ReflexConsoleCanvas"));
        console.ui.input.HistoryContainer.AddItem("Logo", console.logo.text);
        console.ui.input.HistoryContainer.AddItem("cs.version", "es_ui_cs_ready".lang());

        Instance.GetOrCreate<EConsole>();
        Instance.GetOrCreate<EPipe>();
    }
}