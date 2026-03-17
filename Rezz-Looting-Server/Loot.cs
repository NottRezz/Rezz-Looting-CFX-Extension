using CitizenFX.Core;
using Rezz_Looting_Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezz_Looting_Server
{
    internal class Loot
    {
        public int lootId { get; set; }
        public string lootName { get; set; }
        public string lootLabel { get; set; }
        public int lootAmount { get; set; }
        public string lootType { get; set; }
        public Vector3 coords { get; set; }

        public Loot(string lootName, string lootLabel, string lootType, int lootAmount, Vector3 coords)
        {
            this.lootName = lootName;
            this.lootLabel = lootLabel;
            this.lootType = lootType;
            this.lootAmount = lootAmount;
            this.coords = coords; 
        }

        public override string ToString()
        {
            return $"Loot: {lootLabel} ({lootName}) x{lootAmount} at {coords}";
        }
    }

   internal class LootArea
    {
        public int zoneId { get; set; }
        public Vector3 zoneCoords { get; set; }
        public int radius { get; set; }
        public string lootType { get; set; }
        public int maxLoot { get; set; }
        public int spawnChance { get; set; }
        
        public LootArea(int zoneId, Vector3 zoneCoords, int radius, string lootType, int maxLoot, int spawnChance)
        {
            this.zoneId = zoneId;
            this.zoneCoords = zoneCoords;
            this.radius = radius;
            this.lootType = lootType;
            this.maxLoot = maxLoot;
            this.spawnChance = spawnChance;
        }

        public override string ToString()
        {
            return $"Loot Area: {zoneId} ({zoneCoords}) {radius} {lootType}";
        }
    }
}
