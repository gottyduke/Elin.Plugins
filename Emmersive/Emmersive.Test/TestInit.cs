using System.Reflection;
using Cwl.Helper.String;
using Emmersive.Contexts.Memory;

namespace Emmersive.Test;

[TestClass]
public static class TestInit
{
    [AssemblyInitialize]
    public static void Init(TestContext context)
    {
        var elinGamePath = "ElinGamePath".EnvVar;
        var bepInExDlls = Path.Combine(elinGamePath, "BepInEx", "core");
        var elinDlls = Path.Combine(elinGamePath, "Elin_Data", "Managed");

        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            var asmName = new AssemblyName(args.Name).Name + ".dll";

            string[] searchPaths = [bepInExDlls, elinDlls];
            return searchPaths.Select(dir => Path.Combine(dir, asmName))
                .Where(File.Exists)
                .Select(Assembly.LoadFrom)
                .FirstOrDefault();
        };

        LoadMemoryLexicon();
    }

    private static void LoadMemoryLexicon()
    {
        MemoryLexicon.Current = MemoryLexicon.Merge(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MemoryLexicon.json")));
    }
}