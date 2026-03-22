using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezz_Looting_Server.LootObjects
{
    internal class LootAreaSpawnZones
    {

        public int MainZoneId { get; set; }

        public int SubZoneId { get; set; }

        public bool HasLoot { get; set; }

        public Vector3 SpawnCoords { get; set; }

        public string ExclusiveType { get; set; }

        public Loot LootData { get; set; }

        public LootAreaSpawnZones(int MainZoneId, int SubZoneId, Vector3 SpawnCoords, string ExclusiveType)
        {
            this.MainZoneId = MainZoneId;
            this.SubZoneId = SubZoneId;
            this.SpawnCoords = SpawnCoords;
            this.ExclusiveType = ExclusiveType;
            HasLoot = false;
        }
    }
}
