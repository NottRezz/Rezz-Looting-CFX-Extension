using CitizenFX.Core;
using System.Collections.Generic;

namespace Rezz_Looting_Client
{
    public class Config
    {
        public int MaxLoot { get; set; }
        public Dictionary<int, LootArea> LootAreas { get; set; }
        public Dictionary<string, List<LootDefinition>> LootTablesByType { get; set; }
    }

    public class LootArea
    {
        public int ZoneId { get; set; }
        public Vector3 ZoneCoords { get; set; }
        public Dictionary<int, LootAreaSpawnZones> LootSpawns { get; set; }
        public float Radius { get; set; }
        public string LootType { get; set; }
        public int MaxLoot { get; set; }
        public int SpawnChance { get; set; }
        public int LootTier { get; set; }
    }

    public class LootAreaSpawnZones
    {
        public int MainZoneId { get; set; }
        public int SubZoneId { get; set; }
        public Vector3 SpawnCoords { get; set; }
        public string ExclusiveType { get; set; }
    }

    public class LootDefinition
    {
        public string LootName { get; set; }
        public string LootLabel { get; set; }
        public string LootType { get; set; }
        public int MinAmount { get; set; }
        public int MaxAmount { get; set; }
        public int MinTier { get; set; }
        public int MaxTier { get; set; }
    }
}