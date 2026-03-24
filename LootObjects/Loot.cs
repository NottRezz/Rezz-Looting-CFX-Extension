using CitizenFX.Core;
using Rezz_Looting_Server.LootObjects;
using System.Collections.Generic;

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
        public string Loot3dModel { get; set; }
        public int LootEntityId { get; set; }

        public Loot(int LootId, Vector3 Coords, string LootName, string LootLabel, string LootType, int LootAmount, string Loot3dModel)
        {
            this.LootId = LootId;
            this.Coords = Coords;
            this.LootName = LootName;
            this.LootLabel = LootLabel;
            this.LootType = LootType;
            this.LootAmount = LootAmount;
            this.Loot3dModel = Loot3dModel;
        }

        public override string ToString()
        {
            return $"Loot: {LootLabel} ({LootName}) x{LootAmount} at {Coords}";
        }
    }
}