using System;
using System.ComponentModel;
using Cwl.Helper.Unity;

// ReSharper disable InconsistentNaming

namespace Emmersive.API.Plugins.SceneDirector;

[Description("Core plugin that orchestrates scene play.")]
[EmPlugin]
public partial class SceneDirector : EClass
{
    private bool FindSameMapChara(int uid, out Chara chara)
    {
        chara = game.cards.Find(uid) ?? _map.charas.Find(c => c.uid == uid);
        return chara is { isDestroyed: false, ExistsOnMap: true };
    }

    // for thread safety, we don't use async for kernel calls
    // because unity calls must be made on main thread
    public void DeferAction(Action action, float seconds)
    {
        if (seconds == 0f) {
            lock (core.actionsNextFrame) {
                core.actionsNextFrame.Add(action);
            }
        } else {
            CoroutineHelper.Deferred(action, seconds);
        }
    }
}