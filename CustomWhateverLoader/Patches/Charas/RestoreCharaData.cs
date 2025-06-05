using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Cwl.API.Attributes;
using Cwl.Helper.Extensions;
using Cwl.Helper.Runtime;
using Cwl.Helper.Unity;
using Cwl.LangMod;

namespace Cwl.Patches.Charas;

internal class RestoreCharaData
{
    private const string SourceIdEntry = "cwl_source_chara_id";
    private static List<(Chara, SourceChara.Row)>? _restore;

    [CwlCharaOnCreateEvent]
    internal static void SetOrRestoreCharaData(Chara chara)
    {
        if (!chara.mapStr.TryGetValue(SourceIdEntry, out var sourceId)) {
            chara.mapStr.Set(SourceIdEntry, chara.id);
            CwlMod.Debug<RestoreCharaData>($"setting source data {chara.id}");
            return;
        }

        if (chara.id == sourceId) {
            return;
        }

        if (!EMono.sources.charas.map.TryGetValue(sourceId, out var source)) {
            return;
        }

        if (_restore is null) {
            _restore = [];

            CoroutineHelper.Deferred(
                DeferredRestore,
                () => EClass.core.IsGameStarted);
        }

        _restore.Add((chara, source));
    }

    [SwallowExceptions]
    private static void DeferredRestore()
    {
        if (_restore is null) {
            return;
        }

        Dialog.YesNo(
            "cwl_ui_chara_restore".Loc(BuildCharaList(_restore)),
            () => {
                foreach (var (chara, source) in _restore) {
                    chara.id = source.id;
                    chara.source = source;
                    chara.mapStr.Set(SourceIdEntry, source.id);

                    CachedMethods.GetCachedMethod(nameof(Card), "_OnDeserialized", [typeof(StreamingContext)])?
                        .FastInvoke(chara, new StreamingContext());

                    chara.ChangeRace(source.race);

                    CwlMod.Log<RestoreCharaData>("cwl_log_chara_restore".Loc(chara.id, chara.Name));
                }

                _restore = null;
            },
            () => _restore = null,
            "cwl_ui_chara_restore_yes",
            "cwl_ui_chara_restore_no");
    }

    private static string BuildCharaList(List<(Chara, SourceChara.Row)> list)
    {
        var sb = new StringBuilder();

        foreach (var (chara, source) in list.Take(15)) {
            sb.AppendLine($"{chara.NameSimple},{chara.currentZone.Name},lv {chara.LV} => {source.GetText()}");
        }

        if (list.Count > 15) {
            sb.AppendLine($"+ {list.Count - 15}...");
        }

        return sb.ToString().TrimEnd();
    }
}