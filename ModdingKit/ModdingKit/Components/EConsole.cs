using System.Collections.Generic;
using System.Linq;
using ReflexCLI.Attributes;
using ReflexCLI.UI;
using UnityEngine;

namespace EModding.Components;

[ConsoleCommandClassCustomizer("mod")]
internal class EConsole : EMono
{
    private void Update()
    {
        if (scene.mode is not Scene.Mode.Title) {
            return;
        }

        var pressed = Input.GetKeyDown(EClass.core.config.input.keys.console.key);
        if (ReflexUIManager.IsConsoleOpen() && Input.GetKeyDown(KeyCode.Escape)) {
            ReflexUIManager.StaticClose();
        } else if (pressed) {
            ReflexUIManager.StaticOpen();
        }
    }

    #region Data

    /// <summary>
    ///     Reload all sources
    /// </summary>
    [ConsoleCommand("load_sources")]
    internal static void ReloadSources(bool reloadGame = true)
    {
        if (EClass.core.IsGameStarted) {
            if (reloadGame) {
                game.Save(silent: true);
                CoroutineHelper.Deferred(() => Game.Load(Game.id, game.isCloud));
            }

            scene.Init(Scene.Mode.Title);
        }

        sources.Reload();
    }

    #endregion

    #region Util

    /// <summary>
    ///     Get current position info
    /// </summary>
    [ConsoleCommand("pos")]
    private static string GetPos()
    {
        var pos = pc.pos;

        if (_zone.IsRegion) {
            return $"Overworld: {pos.eloX}, {pos.eloY}\n";
        }

        var (zoneType, zoneId, zoneLv) = Zone.ParseZoneFullName(_zone.ZoneFullName);
        return $"Map: {pos.x}, {pos.z}\n" +
               $"Zone: {zoneType} {zoneId} @ {zoneLv} uid: {_zone.uid}\n" +
               $"Expire: {Date.ToDate(_zone.dateExpire)}\n" +
               $"Source: {ModUtil.FindSourceRowPackage(_zone.source)?.title ?? "Elin/Unknown"}";
    }

    /// <summary>
    ///     Enter/Exit dev mode without restarting the game
    /// </summary>
    [ConsoleCommand("elin_dev")]
    private static string SetElinDevMode(bool enable = true)
    {
        var mode = enable ? ReleaseMode.Debug : ReleaseMode.Public;
        core.SetReleaseMode(mode);
        core.debug.enable = enable;

        ReloadSources();
        if (enable) {
            EGui.CreatePopup("to disable DEV mode, use console command\n" +
                             "'mod.elin_dev false'".TagColor(Color.cyan));
        }

        return $"debug : {enable}";
    }

    #endregion

    #region Spawn

    /// <summary>
    ///     Spawn zone at position
    /// </summary>
    [ConsoleCommand("spawn.zone")]
    private static string SpawnZone(string zoneFullName,
                                    int eloX = 99999,
                                    int eloY = 99999,
                                    string parent = "ntyris",
                                    bool force = false)
    {
        var pos = pc.pos;

        if (eloX == 99999) {
            eloX = pos.eloX;
        }

        if (eloY == 99999) {
            eloY = pos.eloY;
        }

        var parentZone = parent == "ntyris" ? game.world.region : ModUtil.FindZoneByFullName(parent);
        if (parentZone is null) {
            return $"Can't find zone parent {parent}";
        }

        var existZone = game.spatials.Find((Zone z) => z.x == eloX && z.y == eloY);
        if (existZone is not null) {
            if (force) {
                existZone.Destroy();
            } else {
                return $"Zone already exists at {eloX}, {eloY} / {existZone}";
            }
        }

        var (_, zoneId, zoneLv) = Zone.ParseZoneFullName(zoneFullName);
        var icon = sources.zones.map.GetValueOrDefault(zoneId)?.pos.TryGet(2, true);

        var zoneTop = SpatialGen.Create(zoneId, parentZone, true, eloX, eloY, icon ?? 306) as Zone;
        var zone = zoneTop?.FindOrCreateLevel(zoneLv);

        if (zone is null) {
            return "Failed to create zone";
        }

        zone.x = eloX;
        zone.y = eloY;

        if (parentZone is Region region) {
            zone.parent?.RemoveChild(zone);
            region.elomap.SetZone(eloX, eloY, zone, true);
        }

        return $"Spawned zone at {eloX}, {eloY} / {zone}";
    }

    /// <summary>
    ///     Remove all cards of same id
    /// </summary>
    [ConsoleCommand("remove.all")]
    private static string RemoveAllCard(string cardId = "chicken")
    {
        var cards = game.cards.globalCharas.Values
            .Concat(_map.Cards)
            .Where(c => c.id == cardId)
            .Distinct()
            .ToArray();

        var destroyed = 0;
        foreach (var card in cards) {
            if (card is Chara chara) {
                chara.DestroyImmediate();
            } else {
                card.Destroy();
            }
            destroyed++;
        }

        return $"Removed {destroyed} cards";
    }

    #endregion
}