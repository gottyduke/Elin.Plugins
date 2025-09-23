using System;

namespace Cwl.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CwlSceneInitEvent(Scene.Mode mode, bool defer = false, bool preInit = false, int order = -1) : CwlEvent
{
    public Scene.Mode Mode => mode;
    public bool Defer => defer;
    public bool PreInit => preInit;
    public int Order => order;
}