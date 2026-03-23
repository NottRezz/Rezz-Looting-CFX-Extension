using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using Rezz_Looting_Server;
using Rezz_Looting_Server.LootObjects;
using System;
using System.Collections.Generic;
using System.Linq;

public class Main : BaseScript
{
    Config config = new Config();

    // Holds live loot state (runtime, not config)
    private Dictionary<int, LootArea> MainLoot = new Dictionary<int, LootArea>();

    private Random rng = new Random();
    private int nextLootId = 1; // ensures unique loot IDs

    public Main()
    {
        InitializeLoot();     // initial spawn
        DebugMainLoot();      // main loop (regen handling)

        API.RegisterCommand("simulateLoot", new Action<int, List<object>, string>(LootObject), false);
        API.RegisterCommand("debugLoot", new Action<int, List<object>, string>(debugLoot), false);
        API.RegisterCommand("jsonifyData", new Action<int, List<object>, string>(GetZones), false);

        BaseScript.Delay(1000);
    }

    private void PlayerZoneChange(Player player, int ZoneId, bool enteringZone)
    {
        Debug.WriteLine($"Zone change: {player.Name} -> {ZoneId} (Entering: {enteringZone})");
        var zone = MainLoot[ZoneId];

        if (!enteringZone)
        {
            if (zone != null)
            {
                if (zone.PlayersInZone.Count <= 0)
                {
                    zone.ZoneLoadState(false);
                }
            }
        }
        else
        {
            if (!MainLoot.ContainsKey(ZoneId))
            {
                Debug.WriteLine($"Zone {ZoneId} not found!");
                return;
            }

            if (!int.TryParse(player.Handle, out int playerId))
            {
                Debug.WriteLine($"Invalid player handle: {player.Handle}");
                return;
            }

            if (!zone.PlayersInZone.Contains(playerId))
            {
                if (zone.PlayersInZone.Count == 0)
                {
                    zone.ZoneLoadState(true);
                }
                zone.PlayersInZone.Add(playerId);
            }
        }
    }

    private void GetZones(int source, List<object> args, string raw)
    {
        var zones = new List<object>();

        foreach (var entry in MainLoot)
        {
            var loot = entry.Value;

            zones.Add(new
            {
                ZoneID = loot.ZoneId,
                ZoneCoords = loot.ZoneCoords,
                Radius = loot.Radius
            });
        }

        string data = JsonConvert.SerializeObject(zones);
        Debug.WriteLine(data);
    }

    // Main loop: handles cooldowns and triggers regen
    private async void DebugMainLoot()
    {
        while (true)
        {
            await BaseScript.Delay(5000);

            var zonesToRegen = new List<int>();

            foreach (var areaEntry in MainLoot)
            {
                var area = areaEntry.Value;

                // countdown active regen timers
                if (area.CanRegen && area.CooldownTimer > 0)
                {
                    area.CooldownTimer -= 5;
                    Debug.WriteLine($"Cooldown Remaining: {area.CooldownTimer}");

                    // queue regen when timer finishes
                    if (area.CooldownTimer <= 0)
                    {
                        area.CanRegen = false;
                        area.CooldownTimer = 0;
                        zonesToRegen.Add(area.ZoneId);
                    }
                }
            }

            // regen happens AFTER loop to avoid modifying collection mid-iteration
            foreach (var zoneId in zonesToRegen)
            {
                Debug.WriteLine("Cooldown Over, Regenerating Loot");
                GenerateLootForZone(zoneId);
            }
        }
    }

    // Debug command: prints all current loot state
    private void debugLoot(int source, List<object> args, string raw)
    {
        Debug.WriteLine("========== MAIN LOOT STATE ==========");

        foreach (var areaEntry in MainLoot)
        {
            int areaId = areaEntry.Key;
            var area = areaEntry.Value;

            Debug.WriteLine($"[AREA {areaId}] Type: {area.LootType} | Tier: {area.LootTier}");

            foreach (var spawnEntry in area.LootSpawns)
            {
                int subZoneId = spawnEntry.Key;
                var spawnZone = spawnEntry.Value;

                // print loot if present
                if (spawnZone.HasLoot && spawnZone.LootData != null)
                {
                    Debug.WriteLine(
                        $"   [SubZone {subZoneId}] LOOT → {spawnZone.LootData.LootLabel} " +
                        $"({spawnZone.LootData.LootName}) x{spawnZone.LootData.LootAmount}"
                    );
                }
                else
                {
                    Debug.WriteLine($"   [SubZone {subZoneId}] EMPTY");
                }
            }
        }

        Debug.WriteLine("=====================================");
    }

    // Simulates looting from a specific zone/subzone
    private void LootObject(int source, List<object> args, string raw)
    {
        // DemoData = Zone 0, SubZone 1

        if (!MainLoot.TryGetValue(1, out var lootArea))
        {
            Debug.WriteLine("Loot area not found.");
            return;
        }

        try
        {
            if (!lootArea.LootSpawns.TryGetValue(0, out var spawnZone))
            {
                Debug.WriteLine("Spawn zone not found.");
                return;
            }

            if (spawnZone.LootData == null)
            {
                Debug.WriteLine("No loot in this spawn zone.");
                return;
            }

            // remove loot from spawn
            var lootData = spawnZone.LootData;
            int objectId = API.NetworkGetEntityFromNetworkId(lootData.LootEntityId);

            if (API.DoesEntityExist(objectId))
            {
                API.DeleteEntity(objectId);
            }

            spawnZone.LootData = null;
            spawnZone.HasLoot = false;

            Debug.WriteLine($"Loot found: {lootData}");

            // check if entire area is now empty
            bool isEmpty = lootArea.LootSpawns.Values.All(z => z.LootData == null);

            if (isEmpty)
            {
                lootArea.CanRegen = true;
                lootArea.CooldownTimer = 10;
                Debug.WriteLine("Loot Area is Empty: Regen Timer Started. Time Remaining: 10s");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading loot data: {ex.Message}");
        }
    }

    // Initial loot generation from config
    private void InitializeLoot()
    {
        MainLoot.Clear();

        foreach (var entry in config.LootAreas)
        {
            var configArea = entry.Value;
            var mainZoneId = entry.Key;

            var runtimeSpawns = new Dictionary<int, LootAreaSpawnZones>();

            foreach (var spawnEntry in configArea.LootSpawns)
            {
                var subZoneId = spawnEntry.Key;
                var configSpawn = spawnEntry.Value;

                runtimeSpawns[subZoneId] = new LootAreaSpawnZones(configSpawn.MainZoneId, configSpawn.SubZoneId, configSpawn.SpawnCoords, configSpawn.ExclusiveType)
                {
                    HasLoot = false,
                    LootData = null
                };
            }

            LootArea tempStorage = new LootArea(mainZoneId, configArea.ZoneCoords, runtimeSpawns, configArea.Radius, configArea.LootType, configArea.MaxLoot, configArea.SpawnChance, configArea.LootTier);

            foreach (var spawnEntry in tempStorage.LootSpawns)
            {
                var subZoneId = spawnEntry.Key;
                var spawnZone = spawnEntry.Value;

                if (spawnZone.HasLoot)
                    continue;

                Debug.WriteLine($"Zone: {tempStorage.ZoneId} Has SubZone Available! ({subZoneId})");

                if (rng.Next(1, 101) > tempStorage.SpawnChance)
                {
                    Debug.WriteLine("No loot spawns, next");
                    continue;
                }

                if (!config.LootTablesByType.TryGetValue(tempStorage.LootType, out var typeLoot))
                    continue;

                var validLoot = typeLoot
                    .Where(lootDef => tempStorage.LootTier >= lootDef.MinTier && tempStorage.LootTier <= lootDef.MaxTier)
                    .ToList();

                if (validLoot.Count == 0)
                {
                    Debug.WriteLine($"No valid loot found for zone {tempStorage.ZoneId}, subzone {subZoneId}");
                    continue;
                }

                int lootId = nextLootId++;
                var selectedLoot = validLoot[rng.Next(validLoot.Count)];

                Loot lootObject = new Loot(lootId, spawnZone.SpawnCoords, selectedLoot.LootName, selectedLoot.LootLabel, selectedLoot.LootType, 1);

                spawnZone.HasLoot = true;
                spawnZone.LootData = lootObject;

                Debug.WriteLine(lootObject.ToString());
            }

            MainLoot[mainZoneId] = tempStorage;
        }
    }

    // Regenerates loot for a specific zone
    private void GenerateLootForZone(int zoneId)
    {
        if (!config.LootAreas.TryGetValue(zoneId, out var area))
            return;

        var id = area.ZoneId;

        // create fresh runtime copy of zone
        var tempStorage = new LootArea(id, area.ZoneCoords, new Dictionary<int, LootAreaSpawnZones>(area.LootSpawns), area.Radius, area.LootType, area.MaxLoot, area.SpawnChance, area.LootTier);

        foreach (var spawnEntry in tempStorage.LootSpawns)
        {
            var spawnZone = spawnEntry.Value;

            if (spawnZone.HasLoot)
                continue;

            if (rng.Next(1, 101) > tempStorage.SpawnChance)
                continue;

            if (!config.LootTablesByType.TryGetValue(tempStorage.LootType, out var typeLoot))
                continue;

            var validLoot = typeLoot.Where(l => tempStorage.LootTier >= l.MinTier && tempStorage.LootTier <= l.MaxTier).ToList();
            if (validLoot.Count == 0)
                continue;

            var selected = validLoot[rng.Next(validLoot.Count)];
            int lootId = nextLootId++;

            var loot = new Loot(lootId, spawnZone.SpawnCoords, selected.LootName, selected.LootLabel, selected.LootType, 1);

            spawnZone.HasLoot = true;
            spawnZone.LootData = loot;

            Debug.WriteLine(loot.ToString());
        }

        // overwrite existing zone with new loot state
        MainLoot[id] = tempStorage;
    }
}