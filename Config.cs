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

    public class ZoneVector3  // Plain C# class, no CitizenFX dependency
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public class LootArea
    {
        public int ZoneId { get; set; }
        public ZoneVector3 ZoneCoords { get; set; }  // ← changed
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
        public ZoneVector3 SpawnCoords { get; set; }  // ← changed
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

    internal class Loot
    {
        public int LootId { get; set; }
        public string LootName { get; set; }
        public string LootLabel { get; set; }
        public int LootAmount { get; set; }
        public string LootType { get; set; }
        public Vector3 Coords { get; set; }
        public string Loot3dModel { get; set; }
        public int LootEntityId { get; set; }
        public override string ToString()
        {
            return $"Loot: {LootLabel} ({LootName}) x{LootAmount} at {Coords}";
        }
    }
}