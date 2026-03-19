using CitizenFX.Core;

namespace Rezz_Looting_Server
{
    internal class LootArea
    {
        public int ZoneId { get; set; }
        public Vector3 ZoneCoords { get; set; }
        public float Radius { get; set; }
        public string LootType { get; set; }
        public int MaxLoot { get; set; }
        public int SpawnChance { get; set; }
        public bool CanRegen { get; set; }
        public int LootTier { get; set; }

        public LootArea(int zoneId, Vector3 zoneCoords, float radius, string lootType, int maxLoot, int spawnChance, int lootTier)
        {
            ZoneId = zoneId;
            ZoneCoords = zoneCoords;
            Radius = radius;
            LootType = lootType;
            MaxLoot = maxLoot;
            SpawnChance = spawnChance;
            LootTier = lootTier;
            CanRegen = false;
        }

        public override string ToString()
        {
            return $"Loot Area: {ZoneId} ({ZoneCoords}) Radius: {Radius} Type: {LootType}";
        }
    }
}