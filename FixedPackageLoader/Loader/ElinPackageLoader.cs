using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.Bootstrap;

namespace PackageLoader.Loader;

internal class ElinPackageLoader(string directory) : UnityChainloader
{
    protected override IList<PluginInfo> DiscoverPlugins()
    {
        return DiscoverPluginsFrom(directory);
    }
}