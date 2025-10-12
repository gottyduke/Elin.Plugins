using System.IO;
using System.Linq;
using System.Reflection;
using Cwl.Helper.FileUtil;

namespace Emmersive.Contexts;

public class SystemContext : ContextProviderBase
{
    private const string DefaultPromptResource = "Emmersive.package.LangMod.EN.Emmersive.SystemPrompt.txt";
    private string _prompt;

    public SystemContext()
    {
        var prompt = PackageIterator.GetRelocatedFilesFromPackage("Emmersive/SystemPrompt.txt").LastOrDefault();

        var watcher = new FileSystemWatcher(prompt!.DirectoryName!, "*.txt") {
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.LastWrite,
        };
        watcher.Changed += (_, e) => {
            if (e.ChangeType == WatcherChangeTypes.Changed) {
                _prompt = File.ReadAllText(prompt.FullName);
                EmMod.Popup<SystemContext>("system prompt reloaded");
            }
        };

        _prompt = File.ReadAllText(prompt.FullName);
    }

    private static string? DefaultPrompt => field ??= GetDefaultPrompt();

    public override string Name => "system_prompt";

    public override object Build()
    {
        return _prompt;
    }

    private static string GetDefaultPrompt()
    {
        using var ms = Assembly.GetExecutingAssembly().GetManifestResourceStream(DefaultPromptResource);
        if (ms is null) {
            return "";
        }

        using var sr = new StreamReader(ms);
        return sr.ReadToEnd();
    }
}