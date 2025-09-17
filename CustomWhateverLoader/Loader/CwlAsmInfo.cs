using System.Reflection;
using Cwl;

[assembly: AssemblyVersion($"{ModInfo.Version}.*")]
[assembly: AssemblyFileVersion(ModInfo.Version)]
[assembly: AssemblyInformationalVersion($"{GitVersionInformation.CommitDate}+{GitVersionInformation.Sha}")]
[assembly: AssemblyProduct($"{ModInfo.Guid}+{GitVersionInformation.BranchName}")]
[assembly: AssemblyTitle($"{ModInfo.Name} [{ModInfo.TargetVersion}]")]
[assembly: AssemblyMetadata("Repo", "https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader")]