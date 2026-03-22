using CitizenFX.Core;
using Rezz_Looting_Server.LootObjects;
using System.Collections.Generic;

namespace Rezz_Looting_Server
{
    internal class LootArea
    {
        public int ZoneId { get; set; }
        public Vector3 ZoneCoords { get; set; }
        public List<LootAreaSpawnZones> LootSpawns { get; set; }
        public float Radius { get; set; }
        public string LootType { get; set; }
        public int MaxLoot { get; set; }
        public int SpawnChance { get; set; }
        public bool CanRegen { get; set; }
        public int LootTier { get; set; }

        public Dictionary<int, Loot> ActiveLoot { get; set; } = new Dictionary<int, Loot>();

        public LootArea(int ZoneId, Vector3 ZoneCoords, List<LootAreaSpawnZones> LootSpawns, float Radius, string LootType, int MaxLoot, int SpawnChance, int LootTier)
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

        public override string ToString()
        {
            return $"Loot Area: {ZoneId} ({ZoneCoords}) Radius: {Radius} Type: {LootType}";
        }
    }
}