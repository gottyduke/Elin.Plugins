using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cwl.API;
using Cwl.API.Processors;
using Cwl.LangMod;
using UnityEngine;
using ViewerMinus.Helper;

namespace ViewerMinus.API;

public class ModListManager : EClass
{
    private static readonly string _path = Path.Combine(Application.persistentDataPath, ModInfo.Name);
    private static readonly GameIOProcessor.GameIOContext _context = new(_path);
    private static FileInfo? _selected;

    private static SerializableModPackage[] CurrentLoaded(bool activated = true)
    {
        return core.mods.packages
            .Where(p => (!activated || (p.activated && p.willActivate)) &&
                        !p.builtin &&
                        !p.id.IsEmpty())
            .Select(p => new SerializableModPackage {
                ModName = p.title,
                ModId = p.id,
            })
            .ToArray();
    }

    public static void EnsurePath()
    {
        var path = Path.Combine(_path, "chunks");
        Directory.CreateDirectory(path);
    }

    public static void SaveModList()
    {
        Dialog.InputName(
            "mvm_ui_input_name",
            "mvm_ui_name_placeholder".Loc(),
            (cancel, text) => {
                if (cancel) {
                    return;
                }

                var list = CurrentLoaded();
#if DEBUG
                _context.SaveUncompressed(list, text);
#else
                _context.Save(list, text);
#endif
                MvMod.LogWithPopup("mvm_ui_save_list".Loc(text));
            });
    }

    public static void LoadModList()
    {
        SelectListInternal(() => {
            if (_selected is null ||
                !_context.Load<SerializableModPackage[]>(out var list, _selected.ShortFilename()) ||
                list is null) {
                return;
            }

            var loaded = CurrentLoaded(false);
            var missing = list.Except(loaded).ToArray();
            if (missing.Length != 0) {
                Dialog.YesNo(
                    "mvm_ui_missing_mods".Loc(BuildMissingList(missing)),
                    () => ApplyModList(list),
                    null,
                    "mvm_ui_load",
                    "mvm_ui_missing_mods_no");
            } else {
                ApplyModList(list);
            }
        });
    }

    public static void RemoveModList()
    {
        SelectListInternal(() => {
            if (_selected is null) {
                return;
            }

            _selected.Delete();
            MvMod.LogWithPopup("mvm_ui_remove_list".Loc(_selected.ShortFilename()));
        });
    }

    public static void RefreshList()
    {
        core.mods.SaveLoadOrder();

        var viewer = LayerMod.Instance;
        if (viewer != null) {
            viewer.list?.List();
        }
    }

    private static void ApplyModList(SerializableModPackage[] list)
    {
        var packages = core.mods.packages;
        Dictionary<string, BaseModPackage> mods = [];
        var builtin = 0;
        foreach (var mod in packages) {
            if (mod.builtin) {
                builtin++;
                continue;
            }

            if (mod.id.IsEmpty()) {
                continue;
            }

            mods[mod.id] = mod;
            mod.willActivate = false;
        }

        List<BaseModPackage> activated = [];
        foreach (var mod in list) {
            if (!mods.TryGetValue(mod.ModId, out var package)) {
                continue;
            }

            package.willActivate = true;
            packages.Remove(package);
            activated.Add(package);
        }

        packages.InsertRange(builtin, activated);
        RefreshList();
    }

    private static string BuildMissingList(IReadOnlyList<SerializableModPackage> missing)
    {
        var sb = new StringBuilder();

        foreach (var mod in missing.Take(15)) {
            sb.AppendLine($"{mod.ModName},  {mod.ModId}");
        }

        if (missing.Count > 15) {
            sb.AppendLine($"+ {missing.Count - 15}...");
        }

        return sb.ToString().TrimEnd();
    }

    private static void SelectListInternal(Action onKill)
    {
        _selected = null;

        var path = Path.Combine(_path, "chunks");
        var extension = "*.chunkc";
#if DEBUG
        extension = "*.chunk";
#endif
        var lists = Directory.GetFiles(path, extension, SearchOption.TopDirectoryOnly)
            .Select(f => new FileInfo(f))
            .ToArray();
        if (lists.Length == 0) {
            Dialog.Ok("mvm_ui_zero_list");
            return;
        }

        Dialog.List(
                "mvm_ui_select_list",
                lists,
                PathHelper.ShortFilename,
                (i, _) => {
                    _selected = lists[i];
                    return true;
                },
                true)
            .SetOnKill(onKill);
    }
}