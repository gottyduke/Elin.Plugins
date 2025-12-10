using System.Reflection;
using ACS;

[assembly: AssemblyVersion($"{ModInfo.Version}.*")]
[assembly: AssemblyFileVersion($"{ModInfo.Version}.{GitVersionInformation.CommitsSinceVersionSource}")]
[assembly: AssemblyInformationalVersion($"{GitVersionInformation.CommitDate}+{GitVersionInformation.Sha}")]
[assembly: AssemblyProduct($"{ModInfo.Guid}+{GitVersionInformation.BranchName}")]
[assembly: AssemblyTitle(ModInfo.Name)]
[assembly: AssemblyMetadata("Repo", "https://github.com/gottyduke/Elin.Plugins/tree/master/AnimatedCustomSprites")]