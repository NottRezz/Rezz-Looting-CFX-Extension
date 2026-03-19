using CitizenFX.Core;

namespace Rezz_Looting_Server
{
    internal class Loot
    {
        public int LootId { get; set; }
        public string LootName { get; set; }
        public string LootLabel { get; set; }
        public int LootAmount { get; set; }
        public string LootType { get; set; }
        public Vector3 Coords { get; set; }

        public Loot(int lootId, string lootName, string lootLabel, string lootType, int lootAmount, Vector3 coords)
        {
            LootId = lootId;
            LootName = lootName;
            LootLabel = lootLabel;
            LootType = lootType;
            LootAmount = lootAmount;
            Coords = coords;
        }

        public override string ToString()
        {
            return $"Loot: {LootLabel} ({LootName}) x{LootAmount} at {Coords}";
        }
    }
}