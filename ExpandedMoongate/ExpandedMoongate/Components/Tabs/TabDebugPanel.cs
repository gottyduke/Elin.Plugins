using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Exm.Components.Tabs;

internal class TabDebugPanel : TabExMoongateBase
{
    public override void OnLayout()
    {
        Button("Clear All Map Cache", () => {
            Directory.Delete(CorePath.ZoneSaveUser, true);
        }).GetComponent<Image>().color = Color.red;
    }
}