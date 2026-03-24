using CitizenFX.Core;
using CitizenFX.Core.Native;
using Rezz_Looting_Server.LootObjects;
using System.Collections.Generic;

namespace Rezz_Looting_Server
{
    internal class LootArea
    {
        public int ZoneId { get; set; }
        public Vector3 ZoneCoords { get; set; }
        public Dictionary<int, LootAreaSpawnZones> LootSpawns { get; set; }
        public float Radius { get; set; }
        public string LootType { get; set; }
        public int MaxLoot { get; set; }
        public int SpawnChance { get; set; }
        public bool CanRegen { get; set; }
        public int CooldownTimer { get; set; }
        public int LootTier { get; set; }
        public Dictionary<int, Loot> ActiveLoot { get; set; } = new Dictionary<int, Loot>();
        public List<int> PlayersInZone { get; set; } = new List<int>();

        public LootArea(int ZoneId, Vector3 ZoneCoords, Dictionary<int, LootAreaSpawnZones> LootSpawns, float Radius, string LootType, int MaxLoot, int SpawnChance, int LootTier)
        {
            this.ZoneId = ZoneId;
            this.ZoneCoords = ZoneCoords;
            this.LootSpawns = LootSpawns;
            this.Radius = Radius;
            this.LootType = LootType;
            this.MaxLoot = MaxLoot;
            this.SpawnChance = SpawnChance;
            this.LootTier = LootTier;
            CanRegen = false;
        }

        public void ZoneLoadState(bool shouldLoad)
        {
            foreach (var entry in LootSpawns)
            {
                var spawnZone = entry.Value;

                if (spawnZone.LootData == null)
                    continue;

                var lootData = spawnZone.LootData;

                if (shouldLoad)
                {
                    int Entity = API.CreateObjectNoOffset((uint)API.GetHashKey(lootData.Loot3dModel), lootData.Coords.X, lootData.Coords.Y, lootData.Coords.Z - (float)1.0, true, true, false);
                    API.FreezeEntityPosition(Entity, true);
                    Debug.WriteLine($"Spawned Entity (NET ID): {Entity}");
                    lootData.LootEntityId = Entity;
                }
                else
                {
                    API.DeleteEntity(lootData.LootEntityId);
                    Debug.WriteLine($"Unloaded Entity (NET ID): {lootData.LootEntityId}");
                    lootData.LootEntityId = 0;
                }
            }

            Debug.WriteLine("Zone State: " + shouldLoad);
        }

        public override string ToString()
        {
            return $"Loot Area: {ZoneId} ({ZoneCoords}) Radius: {Radius} Type: {LootType}";
        }
    }
}