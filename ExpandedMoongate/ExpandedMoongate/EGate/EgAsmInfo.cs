using System.Reflection;
using System.Runtime.CompilerServices;
using EGate;

[assembly: AssemblyVersion($"{ModInfo.Version}.*")]
[assembly: AssemblyFileVersion($"{ModInfo.Version}.{GitVersionInformation.CommitsSinceVersionSource}")]
[assembly: AssemblyInformationalVersion($"{GitVersionInformation.CommitDate}+{GitVersionInformation.Sha}")]
[assembly: AssemblyProduct($"{ModInfo.Guid}+{GitVersionInformation.BranchName}")]
[assembly: AssemblyTitle(ModInfo.Name)]
[assembly: AssemblyMetadata("Repo", "https://github.com/gottyduke/Elin.Plugins/tree/master/ExpandedMoongate")]
[assembly: InternalsVisibleTo("ExpandedMoongate.Test")]