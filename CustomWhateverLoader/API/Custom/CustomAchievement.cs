using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cwl.API.Attributes;
using Cwl.API.Processors;
using Cwl.Helper.FileUtil;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Newtonsoft.Json;
using ReflexCLI.Attributes;
using UnityEngine;

namespace Cwl.API.Custom;

[ConsoleCommandClassCustomizer("cwl.achievement")]
public class CustomAchievement
{
    private static readonly Dictionary<string, SerializableAchievement> _managedTemplates = [];
    private static readonly GameIOProcessor.GameIOContext _context = GameIOProcessor.GetPersistentModContext(ModInfo.Name)!;
    private static HashSet<string> _persistentUnlocks = [];

    private static Sprite DefaultAchievementIcon =>
        field ??= Resources.Load<Sprite>("Media/Graphics/Icon/Achievement/acv_mew");

    [CwlContextVar("custom_achievements")]
    [AllowNull]
    private static Dictionary<string, CustomAchievement> ManagedAchievements
    {
        get => field ??= [];
        set {
            field ??= [];

            if (value is null) {
                return;
            }

            foreach (var (id, achievement) in value) {
                if (_managedTemplates.TryGetValue(id, out var template)) {
                    field[id] = new() {
                        Achievement = template,
                    };
                }

                field[id].IsUnlocked = achievement.IsUnlocked;
                field[id].TimeUnlocked = achievement.TimeUnlocked;
                field[id].Progress = achievement.Progress;
            }
        }
    }

    [JsonProperty]
    public required SerializableAchievement Achievement { get; init; }

    [JsonProperty]
    public bool IsUnlocked { get; private set; }

    [JsonProperty]
    public DateTime? TimeUnlocked { get; private set; }

    [JsonProperty]
    public float Progress { get; private set; }

    public static IReadOnlyCollection<CustomAchievement> AllAchievements => ManagedAchievements.Values;

    public bool HasPrerequisites => Achievement.Prerequisites is { Length: > 0 };
    public bool IsPrerequisiteMet => !HasPrerequisites || GetMissingPrerequisites().Count == 0;
    public bool IsProgressMet => Achievement.AutoUnlockProgress is null || Progress >= Achievement.AutoUnlockProgress;
    public bool IsUnlockable => IsPrerequisiteMet && IsProgressMet;

    public static CustomAchievement AddAchievement(SerializableAchievement achievement)
    {
        if (!_managedTemplates.TryAdd(achievement.Id, achievement)) {
            CwlMod.Warn<CustomAchievement>("cwl_warn_acv_dupe".Loc(achievement.Id));
        }

        CwlMod.Log<CustomAchievement>("cwl_log_acv_new".Loc(achievement.Id));

        return ManagedAchievements[achievement.Id] = new() {
            Achievement = achievement,
        };
    }

    public IReadOnlyList<CustomAchievement> GetPrerequisiteAchievements()
    {
        if (Achievement.Prerequisites is null || Achievement.Prerequisites.Length == 0) {
            return [];
        }

        return Achievement.Prerequisites
            .Select(GetAchievement)
            .OfType<CustomAchievement>()
            .ToArray();
    }

    public IReadOnlyList<string> GetMissingPrerequisites()
    {
        if (Achievement.Prerequisites is null || Achievement.Prerequisites.Length == 0) {
            return [];
        }

        return Achievement.Prerequisites
            .Where(id => GetAchievement(id) is not { IsUnlocked: true })
            .ToArray();
    }

    [ConsoleCommand("add")]
    public static CustomAchievement AddAchievement(string id,
                                                   string name,
                                                   string? description = null,
                                                   string[]? prerequisites = null,
                                                   float? goal = null)
    {
        return AddAchievement(new() {
            Id = id,
            Name = name,
            Description = description,
            Prerequisites = prerequisites,
            AutoUnlockProgress = goal,
        });
    }

    /// <summary>
    ///     Unlock a mod achievement by id
    /// </summary>
    [ConsoleCommand("unlock")]
    public static void Unlock(string id)
    {
        GetAchievement(id)?.Unlock();
    }

    /// <summary>
    ///     Unlock a mod achievement by id unconditionally
    /// </summary>
    [ConsoleCommand("unlock")]
    public static void UnlockForce(string id)
    {
        UnlockPrerequisites(id);
        GetAchievement(id)?.Unlock(force: true);
    }

    /// <summary>
    ///     Unlock a mod achievement by id unconditionally and persistently save its state
    /// </summary>
    [ConsoleCommand("unlock_persistent")]
    public static void UnlockPersistent(string id)
    {
        GetAchievement(id)?.Unlock(persistent: true);
    }

    /// <summary>
    ///     Unlock all prerequisites of a mod achievement by id unconditionally
    /// </summary>
    /// <param name="id"></param>
    [ConsoleCommand("unlock_persistent")]
    public static void UnlockPrerequisites(string id)
    {
        foreach (var prerequisite in GetAchievement(id)?.GetPrerequisiteAchievements() ?? []) {
            prerequisite.Unlock(force: true);
        }
    }

    /// <summary>
    ///     Resets a mod achievement by id, clearing its data
    /// </summary>
    [ConsoleCommand("reset")]
    public static void Reset(string id)
    {
        GetAchievement(id)?.Reset();
    }

    [ConsoleCommand("set_progress")]
    public static void SetProgress(string id, float progress)
    {
        GetAchievement(id)?.SetProgress(progress);
    }

    [ConsoleCommand("set_progress")]
    public static void ModProgress(string id, float progressMod)
    {
        GetAchievement(id)?.ModProgress(progressMod);
    }

    [ConsoleCommand("get_progress")]
    public static float GetProgress(string id)
    {
        return GetAchievement(id)?.Progress ?? 0f;
    }

    public static CustomAchievement? GetAchievement(string id)
    {
        return ManagedAchievements.GetValueOrDefault(id);
    }

    [ConsoleCommand("reimport")]
    public static void ReimportAchievementDefinitions()
    {
        _managedTemplates.Clear();

        var definitions = PackageIterator.GetRelocatedFilesFromPackage("Data/achievement.json");
        foreach (var definition in definitions) {
            if (!ConfigCereal.ReadConfig<SerializableAchievement[]>(definition.FullName, out var achievements)) {
                continue;
            }

            foreach (var achievement in achievements) {
                AddAchievement(achievement);
            }
        }

        if (_context.Load<HashSet<string>>(out var persistent, "custom_achievements_persistent")) {
            _persistentUnlocks = persistent;
        }
    }

    public void Unlock(bool persistent = false, bool force = false)
    {
        if (IsUnlocked) {
            return;
        }

        if (!IsUnlockable && !force) {
            return;
        }

        IsUnlocked = true;
        Progress = Achievement.AutoUnlockProgress ?? Progress;
        TimeUnlocked = DateTime.UtcNow;

        if (persistent) {
            if (!_persistentUnlocks.Add(Achievement.Id)) {
                return;
            }

            _context.Save(_persistentUnlocks, "custom_achievements_persistent");
        }

        var popup = "sys_acv".lang(Achievement.Name);
        if (Achievement.Description is { } description) {
            popup += $"\n{description}";
        }

        var icon = $"acv_{Achievement.Id}".LoadSprite() ??
                   $"achievement_{Achievement.Id}".LoadSprite() ??
                   DefaultAchievementIcon;

        EClass.ui.Say(popup, icon);

        CwlMod.Log<CustomAchievement>("cwl_log_acv_unlock".Loc(Achievement.Id));
    }

    public void Reset()
    {
        IsUnlocked = false;
        Progress = 0f;
        TimeUnlocked = null;

        _persistentUnlocks.Remove(Achievement.Id);

        CwlMod.Log<CustomAchievement>("cwl_log_acv_reset".Loc(Achievement.Id));
    }

    public void SetProgress(float progress)
    {
        Progress = progress;

        Unlock();
    }

    public void ModProgress(float progressMod)
    {
        SetProgress(Progress + progressMod);
    }
}