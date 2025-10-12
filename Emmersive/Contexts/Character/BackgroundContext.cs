using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Cwl.Patches.Charas;

namespace Emmersive.Contexts;

public class BackgroundContext(Chara chara) : FileContextBase<BackgroundContext.BackgroundPrompt>
{
    public override string Name => "background";

    [field: AllowNull]
    public static BackgroundContext Default => field ??= new(null!);

    public override object? Build()
    {
        if (Lookup is null) {
            Init();
        }

        var id = chara.IsPC ? "player" : chara.id;
        var background = GetContext(id)?.Prompt;
        if (background is not null) {
            return background;
        }

        return chara.IsPC
            ? EClass.player.GetBackgroundText()
            : BioOverridePatch.GetNpcBackground(null!, chara);
    }

    protected override BackgroundPrompt LoadFromFile(FileInfo file)
    {
        var path = file.FullName;
        var id = Path.GetFileNameWithoutExtension(path);
        return new(id, File.ReadAllText(path), file);
    }

    public static void Init()
    {
        Lookup = Default.LoadAllContexts("Emmersive/Characters").ToLookup(ctx => ctx.CharaId);
    }

    public static void Clear()
    {
        Lookup = null!;
        Overrides.Clear();
    }

    public record BackgroundPrompt(string CharaId, string Prompt, FileInfo Provider);
}