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
    private Config config;
    private Dictionary<int, LootArea> MainLoot = new Dictionary<int, LootArea>();
    private Dictionary<int, int> PlayersInZones = new Dictionary<int, int>();

    private Random rng = new Random();
    private int nextLootId = 1; // ensures unique loot IDs

    public dynamic Core;

    public Main()
    {
        string json = API.LoadResourceFile(API.GetCurrentResourceName(), "config.json");
        config = JsonConvert.DeserializeObject<Config>(json);

        InitializeLoot();     // initial spawn
        DebugMainLoot();      // main loop (regen handling)

        API.RegisterCommand("debugLoot", new Action<int, List<object>, string>(debugLoot), false);
        API.RegisterCommand("jsonifyData", new Action<int, List<object>, string>(GetZones), false);

        EventHandlers["rezz_loot:server:enteredZone"] += new Action<Player, int, bool>(PlayerZoneChange);
        EventHandlers["onResourceStop"] += new Action<string>(OnResourceStop);
        EventHandlers["playerDropped"] += new Action<Player, string, string, uint>(OnPlayerDropped);
        EventHandlers["rezz_looting:server:LootObjet"] += new Action<Player, int, int>(LootObject);

        Core = Exports["vorp_core"].GetCore();

        BaseScript.Delay(1000);
    }

    private async void OnPlayerDropped([FromSource] Player player, string reason, string resourceName, uint clientDropReason)
    {
        Debug.WriteLine($"Player {player.Name} dropped (Reason: {reason}, Resource: {resourceName}, Client Drop Reason: {clientDropReason}).");
        int source = int.Parse(player.Handle);
        await RemovePlayerFromZone(source);
    }

    private async void PlayerZoneChange([FromSource] Player player, int ZoneId, bool enteringZone)
    {
        if (!int.TryParse(player.Handle, out int playerId))
        {
            Debug.WriteLine($"Invalid player handle: {player.Handle}");
            return;
        }

        if (!MainLoot.ContainsKey(ZoneId))
        {
            Debug.WriteLine($"Zone {ZoneId} not found!");
            return;
        }

        var zone = MainLoot[ZoneId];

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
    private async void LootObject([FromSource] Player player, int passedNetId, int passedZoneId)
    {
        // DemoData = Zone 0, SubZone 1
        Debug.WriteLine($"{player.Handle} {passedNetId} {passedZoneId}");
        var ActiveZone = MainLoot[passedZoneId];
        var LootData = ActiveZone.LootSpawns;

        int Zone = passedZoneId;
        int SubZone = 0;



        if (!MainLoot.TryGetValue(Zone, out var lootArea))
        {
            Debug.WriteLine("Loot area not found.");
            return;
        }

        foreach (var entry in LootData)
        {
            var data = entry.Value;

            if (data.LootData != null && data.LootData.LootEntityId == passedNetId)
            {
                SubZone = entry.Key;
                break;
            }
        }

        try
        {
            if (!lootArea.LootSpawns.TryGetValue(SubZone, out var spawnZone))
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
            API.DeleteEntity(API.NetworkGetEntityFromNetworkId(passedNetId));

            dynamic vorpInventory = Exports["vorp_inventory"];

            vorpInventory.addItem(player.Handle, lootData.LootName, lootData.LootAmount);
            Core.NotifyRightTip(player.Handle, $"+ x{lootData.LootAmount} {lootData.LootLabel}", 5000);

            // check if entire area is now empty
            bool isEmpty = lootArea.LootSpawns.Values.All(z => z.LootData == null);

            if (isEmpty)
            {
                lootArea.CanRegen = true;
                lootArea.CooldownTimer = 10;
                await MainLoot[Zone].ZoneLoadState(false);
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
            foreach (var lootData in loot.LootSpawns)
            {
                var data = lootData.Value;
                var lootObj = data.LootData;

                if (lootObj != null)
                {
                    int entity = API.NetworkGetEntityFromNetworkId(lootObj.LootEntityId);
                    API.DeleteEntity(entity);
                }
                
            }
        }
        Debug.WriteLine($"[{resourceName}] All loot spawns deleted and cleared.");
    }

    #region Helper Functions

    private async System.Threading.Tasks.Task RemovePlayerFromZone(int playerId)
    {
        if (PlayersInZones.ContainsKey(playerId))
        {
            int zoneId = PlayersInZones[playerId];
            var zone = MainLoot[zoneId];
            zone.PlayersInZone.Remove(playerId);
            PlayersInZones.Remove(playerId);

            if (zone.PlayersInZone.Count <= 0)
            {
                await zone.ZoneLoadState(false);
            }
        }
    }

    private string SerializeLootForZone(int zoneId)
    {
        var TempLoot = new Dictionary<int, object>();

        foreach (var entry in MainLoot[zoneId].LootSpawns)
        {
            TempLoot.Add(entry.Key, entry.Value.LootData);
        }

        return JsonConvert.SerializeObject(TempLoot);
    }

    private void SendLootDataToPlayer(int zoneId, string playerHandle)
    {
        string SerializedLoot = SerializeLootForZone(zoneId);
        TriggerClientEvent("rezz_looting:client:RecieveLootData", playerHandle, SerializedLoot);
    }

    private void SendLootDataToPlayers(int zoneId, string serializedLoot)
    {
        for (int i = 0; i < MainLoot[zoneId].PlayersInZone.Count; i++)
        {
            TriggerClientEvent("rezz_looting:client:RecieveLootData", MainLoot[zoneId].PlayersInZone[i], serializedLoot);
        }
    }

    private void GenerateLootForSpawnZones(LootArea lootArea)
    {
        foreach (var spawnEntry in lootArea.LootSpawns)
        {
            var subZoneId = spawnEntry.Key;
            var spawnZone = spawnEntry.Value;

            if (spawnZone.HasLoot)
                continue;

            Debug.WriteLine($"Zone: {lootArea.ZoneId} Has SubZone Available! ({subZoneId})");

            if (rng.Next(1, 101) > lootArea.SpawnChance)
            {
                Debug.WriteLine("No loot spawns, next");
                continue;
            }

            if (!config.LootTablesByType.TryGetValue(lootArea.LootType, out var typeLoot))
                continue;

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

            Loot lootObject = new Loot(lootId, spawnZone.SpawnCoords, selectedLoot.LootName, selectedLoot.LootLabel, selectedLoot.LootType, 1, selectedLoot.Loot3dModel);

            spawnZone.HasLoot = true;
            spawnZone.LootData = lootObject;

            Debug.WriteLine(lootObject.ToString());
        }
    }

    #endregion

    // Regenerates loot for a specific zone
    private async void GenerateLootForZone(int zoneId)
    {
        if (!config.LootAreas.TryGetValue(zoneId, out var area))
            return;

        var id = area.ZoneId;

        // preserve the existing players in zone
        var existingPlayers = new List<int>(MainLoot[id].PlayersInZone);

        // create fresh runtime copy of zone
        var tempStorage = new LootArea(id, area.ZoneCoords, new Dictionary<int, LootAreaSpawnZones>(area.LootSpawns), area.Radius, area.LootType, area.MaxLoot, area.SpawnChance, area.LootTier);

        GenerateLootForSpawnZones(tempStorage);

        // overwrite existing zone with new loot state
        MainLoot[id] = tempStorage;

        // restore the players in zone
        MainLoot[id].PlayersInZone = existingPlayers;

        await MainLoot[id].ZoneLoadState(true);

        string SerializedLoot = SerializeLootForZone(id);
        SendLootDataToPlayers(id, SerializedLoot);
    }
}