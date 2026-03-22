using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezz_Looting_Server
{
    internal class Config : BaseScript
    {
        public Dictionary<int, LootArea> LootAreas;

        public int MaxLoot;

        public Config()
        {
            MaxLoot = 100;

            LootAreas = new Dictionary<int, LootArea>
            {
                {1, new LootArea(1, new Vector3(-5647.97f, -3417.02f, -22.63f), 5, "Food", 5, 50, 1)}, //ZoneID, LootArea (zoneId (int), zoneCoords (Vector3), radius (int), lootType (string), maxLoot (int), spawnChance (int), Loot Tier (int)
            };
        }
    }
}
