using CitizenFX.Core;
using CitizenFX.Core.Native;
using Rezz_Looting_Server.LootObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Rezz_Looting_Server
{
    public class Main : BaseScript
    {
        Config config = new Config();

        private Dictionary<int, LootArea> MainLoot = new Dictionary<int, LootArea>();

        private Random rng = new Random();

        private int nextLootId = 1;

        public Main()
        {
            InitializeLoot();
            DebugMainLoot();

            API.RegisterCommand("simulateLoot", new Action<int, List<object>, string>(SimulateLootCommand), false);

            API.RegisterCommand("debugLoot", new Action<int, List<object>, string>(debugLoot), false);

            BaseScript.Delay(1000);
        }

        private async void DebugMainLoot()
        {
            while (true)
            {
                await BaseScript.Delay(5000);

                var zonesToRegen = new List<int>();

                foreach (var areaEntry in MainLoot)
                {
                    var area = areaEntry.Value;

                    if (area.CanRegen && area.CooldownTimer > 0)
                    {
                        area.CooldownTimer -= 5;
                        Debug.WriteLine($"Cooldown Remaining: {area.CooldownTimer}");

                        if (area.CooldownTimer <= 0)
                        {
                            area.CanRegen = false;
                            area.CooldownTimer = 0;
                            zonesToRegen.Add(area.ZoneId);
                        }
                    }
                }

                // 🔥 modify AFTER loop
                foreach (var zoneId in zonesToRegen)
                {
                    Debug.WriteLine("Cooldown Over, Regenerating Loot");
                    GenerateLootForZone(zoneId);
                }
            }
        }

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

        private void SimulateLootCommand(int source, List<object> args, string raw)
        {
            // DemoData = Zone 0, SubZone 1

            if (!MainLoot.TryGetValue(0, out var lootArea))
            {
                Debug.WriteLine("Loot area not found.");
                return;
            }

            try
            {
                if (!lootArea.LootSpawns.TryGetValue(1, out var spawnZone))
                {
                    Debug.WriteLine("Spawn zone not found.");
                    return;
                }

                if (spawnZone.LootData == null)
                {
                    Debug.WriteLine("No loot in this spawn zone.");
                    return;
                }

                var lootData = spawnZone.LootData;
                spawnZone.LootData = null;
                spawnZone.HasLoot = false;

                Debug.WriteLine($"Loot found: {lootData}");

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

        private void InitializeLoot()
        {
            foreach (var entry in config.LootAreas)
            {
                var area = entry.Value;
                var id = entry.Key;
                LootArea TempStorage = new LootArea(id, area.ZoneCoords, area.LootSpawns, area.Radius, area.LootType, area.MaxLoot, area.SpawnChance, area.LootTier);

                for (int i = 0; i < area.LootSpawns.Count; i++)
                {
                    int lootId = nextLootId++;
                    if (!area.LootSpawns[i].HasLoot)
                    {
                        Debug.WriteLine($"Zone: {area.ZoneId} Has SubZone Available! ");
                        if (rng.Next(1, 101) <= area.SpawnChance)
                        {
                            if (!config.LootTablesByType.TryGetValue(area.LootType, out var typeLoot))
                                continue;

                            var validLoot = typeLoot
                            .Where(lootDef => area.LootTier >= lootDef.MinTier && area.LootTier <= lootDef.MaxTier)
                            .ToList();

                            var selectedLoot = validLoot[rng.Next(validLoot.Count)];

                            Loot LootObject = new Loot(i, area.LootSpawns[i].SpawnCoords, selectedLoot.LootName, selectedLoot.LootLabel, selectedLoot.LootType, 1); // NEED TO REVISE ID ASSIGNMENT, JUST TESTING
                            
                            area.LootSpawns[i].HasLoot = true;
                            area.LootSpawns[i].LootData = LootObject;

                            Debug.WriteLine(LootObject.ToString());
                        }
                        else
                        {
                            Debug.WriteLine($"No loot spawns, next");
                        }
                    }
                }
                MainLoot.Add(id, TempStorage);
            }
        }

        private void GenerateLootForZone(int zoneId)
        {
            if (!config.LootAreas.TryGetValue(zoneId, out var area))
                return;

            var id = area.ZoneId;

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

            MainLoot[id] = tempStorage;
        }
    }
}
