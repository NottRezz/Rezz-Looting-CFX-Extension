using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using Rezz_Looting_Server;
using Rezz_Looting_Server.LootObjects;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Policy;

public class Main : BaseScript
{
    #region Constants
    private const int TICK_DELAY_MS = 5000;
    private const int COOLDOWN_DECREMENT = 5;
    private const int DEFAULT_REGEN_COOLDOWN = 20;
    private const int RNG_MAX_VALUE = 101;
    private const int DEFAULT_LOOT_AMOUNT = 1;
    private const int NOTIFICATION_DURATION_MS = 5000;
    private const int STARTUP_DELAY_MS = 1000;
    #endregion

    private Config config;
    private Dictionary<int, LootArea> MainLoot = new Dictionary<int, LootArea>();
    private Dictionary<int, int> PlayersInZones = new Dictionary<int, int>();

    private Random rng = new Random();
    private int nextLootId = 1;

    public dynamic Core;

    public Main()
    {
        try
        {
            string json = API.LoadResourceFile(API.GetCurrentResourceName(), "config.json");
            if (string.IsNullOrEmpty(json))
            {
                Debug.WriteLine("[ERROR] Failed to load config.json");
                return;
            }

            config = JsonConvert.DeserializeObject<Config>(json);
            if (config == null)
            {
                Debug.WriteLine("[ERROR] Failed to deserialize config.json");
                return;
            }

            InitializeLoot();
            DebugMainLoot();

            API.RegisterCommand("debugLoot", new Action<int, List<object>, string>(DebugLoot), false);
            API.RegisterCommand("jsonifyData", new Action<int, List<object>, string>(GetZones), false);

            EventHandlers["rezz_loot:server:enteredZone"] += new Action<Player, int, bool>(PlayerZoneChange);
            EventHandlers["onResourceStop"] += new Action<string>(OnResourceStop);
            EventHandlers["playerDropped"] += new Action<Player, string, string, uint>(OnPlayerDropped);
            EventHandlers["rezz_looting:server:LootObjet"] += new Action<Player, int, int>(LootObject);

            Core = Exports["vorp_core"].GetCore();

            BaseScript.Delay(STARTUP_DELAY_MS);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Failed to initialize Main: {ex.Message}");
        }
    }

    private async void OnPlayerDropped([FromSource] Player player, string reason, string resourceName, uint clientDropReason)
    {
        try
        {
            Debug.WriteLine($"Player {player.Name} dropped (Reason: {reason}, Resource: {resourceName}, Client Drop Reason: {clientDropReason}).");

            if (!int.TryParse(player.Handle, out int source))
            {
                Debug.WriteLine($"[ERROR] Invalid player handle on disconnect: {player.Handle}");
                return;
            }

            await RemovePlayerFromZone(source);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] OnPlayerDropped failed: {ex.Message}");
        }
    }

    private async void PlayerZoneChange([FromSource] Player player, int ZoneId, bool enteringZone)
    {
        try
        {
            if (!int.TryParse(player.Handle, out int playerId))
            {
                Debug.WriteLine($"[ERROR] Invalid player handle: {player.Handle}");
                return;
            }

            if (!TryGetZone(ZoneId, out var zone))
            {
                Debug.WriteLine($"[ERROR] Zone {ZoneId} not found!");
                return;
            }

            Debug.WriteLine($"Zone change: {player.Name} -> {ZoneId} (Entering: {enteringZone})");

            if (!enteringZone)
            {
                await RemovePlayerFromZone(playerId);
            }
            else
            {
                if (!zone.PlayersInZone.Contains(playerId))
                {
                    if (zone.PlayersInZone.Count == 0)
                    {
                        await zone.ZoneLoadState(true);
                    }

                    SendLootDataToPlayer(ZoneId, player.Handle);
                    zone.PlayersInZone.Add(playerId);

                    if (!PlayersInZones.ContainsKey(playerId))
                    {
                        PlayersInZones.Add(playerId, ZoneId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] PlayerZoneChange failed: {ex.Message}");
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
            await BaseScript.Delay(TICK_DELAY_MS);

            var zonesToRegen = new List<int>();

            foreach (var areaEntry in MainLoot)
            {
                var area = areaEntry.Value;

                if (area.CanRegen && area.CooldownTimer > 0)
                {
                    area.CooldownTimer -= COOLDOWN_DECREMENT;
                    Debug.WriteLine($"Cooldown Remaining: {area.CooldownTimer}");

                    if (area.CooldownTimer <= 0)
                    {
                        area.CanRegen = false;
                        area.CooldownTimer = 0;
                        zonesToRegen.Add(area.ZoneId);
                    }
                }
            }

            foreach (var zoneId in zonesToRegen)
            {
                Debug.WriteLine("Cooldown Over, Regenerating Loot");
                GenerateLootForZone(zoneId);
            }
        }
    }

    private void DebugLoot(int source, List<object> args, string raw)
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

    private async void LootObject([FromSource] Player player, int passedNetId, int passedZoneId)
    {
        try
        {
            Debug.WriteLine($"Loot attempt: Player {player.Handle} | NetID {passedNetId} | Zone {passedZoneId}");

            if (!TryGetZone(passedZoneId, out var lootArea))
            {
                Debug.WriteLine("[ERROR] Loot area not found.");
                return;
            }

            int subZone = FindSubZoneByNetId(lootArea.LootSpawns, passedNetId);
            if (subZone == -1)
            {
                Debug.WriteLine("[ERROR] No matching loot found for NetID.");
                return;
            }

            if (!lootArea.LootSpawns.TryGetValue(subZone, out var spawnZone))
            {
                Debug.WriteLine("[ERROR] Spawn zone not found.");
                return;
            }

            if (spawnZone.LootData == null)
            {
                Debug.WriteLine("[ERROR] No loot in this spawn zone.");
                return;
            }

            var lootData = spawnZone.LootData;
            int objectId = API.NetworkGetEntityFromNetworkId(lootData.LootEntityId);

            if (API.DoesEntityExist(objectId))
            {
                API.DeleteEntity(objectId);
            }

            spawnZone.LootData = null;
            spawnZone.HasLoot = false;

            Debug.WriteLine($"Loot collected: {lootData}");

            dynamic vorpInventory = Exports["vorp_inventory"];
            vorpInventory.addItem(player.Handle, lootData.LootName, lootData.LootAmount);
            Core.NotifyRightTip(player.Handle, $"+ x{lootData.LootAmount} {lootData.LootLabel}", NOTIFICATION_DURATION_MS);

            bool isEmpty = lootArea.LootSpawns.Values.All(z => z.LootData == null);

            if (isEmpty)
            {
                lootArea.CanRegen = true;
                lootArea.CooldownTimer = DEFAULT_REGEN_COOLDOWN;
                await lootArea.ZoneLoadState(false);
                Debug.WriteLine($"Loot Area is Empty: Regen Timer Started. Time Remaining: {DEFAULT_REGEN_COOLDOWN}s");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] LootObject failed: {ex.Message}");
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

            GenerateLootForSpawnZones(tempStorage);

            MainLoot[mainZoneId] = tempStorage;
        }
    }

    private void OnResourceStop(string resourceName)
    {
        if (API.GetCurrentResourceName() != resourceName)
            return;

        Debug.WriteLine($"[{resourceName}] Resource stopping, deleting all loot spawns...");

        foreach (var entry in MainLoot)
        {
            var loot = entry.Value;
            foreach (var spawnZone in loot.LootSpawns.Values)
            {
                if (spawnZone.LootData != null)
                {
                    int entity = API.NetworkGetEntityFromNetworkId(spawnZone.LootData.LootEntityId);
                    if (API.DoesEntityExist(entity))
                    {
                        API.DeleteEntity(entity);
                    }
                }
            }
        }
        Debug.WriteLine($"[{resourceName}] All loot spawns deleted and cleared.");
    }

    #region Helper Functions

    private bool TryGetZone(int zoneId, out LootArea zone)
    {
        return MainLoot.TryGetValue(zoneId, out zone);
    }

    private int FindSubZoneByNetId(Dictionary<int, LootAreaSpawnZones> lootSpawns, int netId)
    {
        foreach (var entry in lootSpawns)
        {
            if (entry.Value.LootData != null && entry.Value.LootData.LootEntityId == netId)
            {
                return entry.Key;
            }
        }
        return -1;
    }

    private async System.Threading.Tasks.Task RemovePlayerFromZone(int playerId)
    {
        if (PlayersInZones.TryGetValue(playerId, out int zoneId))
        {
            if (TryGetZone(zoneId, out var zone))
            {
                zone.PlayersInZone.Remove(playerId);
                PlayersInZones.Remove(playerId);

                if (zone.PlayersInZone.Count == 0)
                {
                    await zone.ZoneLoadState(false);
                }
            }
        }
    }

    private string SerializeLootForZone(int zoneId)
    {
        if (!TryGetZone(zoneId, out var zone))
        {
            return JsonConvert.SerializeObject(new Dictionary<int, object>());
        }

        var lootDictionary = new Dictionary<int, object>();

        foreach (var entry in zone.LootSpawns)
        {
            lootDictionary.Add(entry.Key, entry.Value.LootData);
        }

        return JsonConvert.SerializeObject(lootDictionary);
    }

    private void SendLootDataToPlayer(int zoneId, string playerHandle)
    {
        string serializedLoot = SerializeLootForZone(zoneId);
        TriggerClientEvent("rezz_looting:client:RecieveLootData", playerHandle, serializedLoot);
    }

    private void SendLootDataToPlayers(int zoneId, string serializedLoot)
    {
        if (!TryGetZone(zoneId, out var zone))
            return;

        foreach (var playerId in zone.PlayersInZone)
        {
            TriggerClientEvent("rezz_looting:client:RecieveLootData", playerId, serializedLoot);
        }
    }

    private void GenerateLootForSpawnZones(LootArea lootArea)
    {
        if (lootArea == null || config?.LootTablesByType == null)
            return;

        foreach (var spawnEntry in lootArea.LootSpawns)
        {
            var subZoneId = spawnEntry.Key;
            var spawnZone = spawnEntry.Value;

            if (spawnZone.HasLoot)
                continue;

            Debug.WriteLine($"Zone: {lootArea.ZoneId} Has SubZone Available! ({subZoneId})");

            if (rng.Next(1, RNG_MAX_VALUE) > lootArea.SpawnChance)
            {
                Debug.WriteLine("Spawn chance failed, skipping");
                continue;
            }

            if (!config.LootTablesByType.TryGetValue(lootArea.LootType, out var typeLoot))
            {
                Debug.WriteLine($"[ERROR] No loot table found for type: {lootArea.LootType}");
                continue;
            }

            var validLoot = typeLoot
                .Where(lootDef => lootArea.LootTier >= lootDef.MinTier && lootArea.LootTier <= lootDef.MaxTier)
                .ToList();

            if (validLoot.Count == 0)
            {
                Debug.WriteLine($"No valid loot found for zone {lootArea.ZoneId}, subzone {subZoneId}");
                continue;
            }

            int lootId = nextLootId++;
            var selectedLoot = validLoot[rng.Next(validLoot.Count)];

            Loot lootObject = new Loot(lootId, spawnZone.SpawnCoords, selectedLoot.LootName, selectedLoot.LootLabel, selectedLoot.LootType, DEFAULT_LOOT_AMOUNT, selectedLoot.Loot3dModel);

            spawnZone.HasLoot = true;
            spawnZone.LootData = lootObject;

            Debug.WriteLine(lootObject.ToString());
        }
    }

    #endregion

    private async System.Threading.Tasks.Task GenerateLootForZone(int zoneId)
    {
        try
        {
            if (config?.LootAreas == null || !config.LootAreas.TryGetValue(zoneId, out var area))
            {
                Debug.WriteLine($"[ERROR] Cannot regenerate loot for zone {zoneId}: config area not found");
                return;
            }

            if (!TryGetZone(zoneId, out var currentZone))
            {
                Debug.WriteLine($"[ERROR] Cannot regenerate loot for zone {zoneId}: zone not found in MainLoot");
                return;
            }

            var existingPlayers = new List<int>(currentZone.PlayersInZone);

            var tempStorage = new LootArea(zoneId, area.ZoneCoords, new Dictionary<int, LootAreaSpawnZones>(area.LootSpawns), area.Radius, area.LootType, area.MaxLoot, area.SpawnChance, area.LootTier);

            GenerateLootForSpawnZones(tempStorage);

            MainLoot[zoneId] = tempStorage;
            MainLoot[zoneId].PlayersInZone = existingPlayers;

            if (existingPlayers.Count > 0)
            {
                await MainLoot[zoneId].ZoneLoadState(true);
                string serializedLoot = SerializeLootForZone(zoneId);
                SendLootDataToPlayers(zoneId, serializedLoot);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] GenerateLootForZone failed for zone {zoneId}: {ex.Message}");
        }
    }
}