using System.Reflection;
using System.Runtime.CompilerServices;
using Emmersive;

[assembly: AssemblyVersion($"{ModInfo.Version}.*")]
[assembly: AssemblyFileVersion(ModInfo.Version)]
[assembly: AssemblyInformationalVersion($"{GitVersionInformation.CommitDate}+{GitVersionInformation.Sha}")]
[assembly: AssemblyProduct($"{ModInfo.Guid}+{GitVersionInformation.BranchName}")]
[assembly: AssemblyTitle(ModInfo.Name)]
[assembly: AssemblyMetadata("Repo", "https://github.com/gottyduke/Elin.Plugins/tree/master/Emmersive")]
[assembly: InternalsVisibleTo("Emmersive.Test")]