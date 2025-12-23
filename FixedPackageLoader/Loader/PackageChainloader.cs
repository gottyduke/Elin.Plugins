using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace PackageLoader.Loader;

public static class ModInfo
{
    public const string Guid = "elin.plugins.chainloader";
    public const string Name = "Package Chainloader";
    public const string Version = "2.0.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
internal class PackageChainloader : BaseUnityPlugin
{
    private void Awake()
    {
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModInfo.Guid);
        StartCoroutine(DelayedStart());
    }

    private static IEnumerator DelayedStart()
    {
        while (!ModManager.IsInitialized) {
            yield return null;
        }

        foreach (var path in ModManager.ListChainLoad) {
            Debug.Log($"Chain Loading Scripts: {path}");

            var loader = new ElinPackageLoader(path);
            loader.Execute();

            foreach (var info in loader.Plugins.Values) {
                ModManager.ListPluginObject.Add(info.Instance);
            }

            if (loader.Plugins.Values.Count > 0) {
                // also allow external assembly plugin info resolve
                TypeLoader.CecilResolver.AddSearchDirectory(path);
            }
        }
    }
}