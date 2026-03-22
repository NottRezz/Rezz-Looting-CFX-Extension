using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Rezz_Looting_Server
{
    public class Main : BaseScript
    {
        Config config = new Config();

        private Dictionary<int, LootArea> MainLoot = new Dictionary<int, LootArea>();

        private Random rng = new Random();

        private int nextLootId = 1;

        public Main()
        {
            InitializeLoot();
        }

        private void InitializeLoot()
        {
            foreach (var entry in config.LootAreas)
            {
                var area = entry.Value;
                var id = entry.Key;
                LootArea TempStorage = new LootArea(id, area.ZoneCoords, area.LootSpawns, area.Radius, area.LootType, area.MaxLoot, area.SpawnChance, area.LootTier);

                for (int i = 0; i < area.LootSpawns.Count; i++)
                {
                    if (!area.LootSpawns[i].HasLoot)
                    {
                        Debug.WriteLine($"Zone: {area.ZoneId} Has SubZone Available! ");
                        if (rng.Next(1, 101) <= area.SpawnChance)
                        {
                            if (!config.LootTablesByType.TryGetValue(area.LootType, out var typeLoot))
                                continue;

                            var validLoot = typeLoot
                            .Where(lootDef => area.LootTier >= lootDef.MinTier && area.LootTier <= lootDef.MaxTier)
                            .ToList();

                            var selectedLoot = validLoot[rng.Next(validLoot.Count)];

                            Loot LootObject = new Loot(i, area.LootSpawns[i].SpawnCoords, selectedLoot.LootName, selectedLoot.LootLabel, selectedLoot.LootType, 1);

                            Debug.WriteLine(LootObject.ToString());
                        }
                        else
                        {
                            Debug.WriteLine($"No loot spawns, next");
                        }
                    }
                    int lootId = nextLootId++;
                    MainLoot.Add(id, TempStorage);
                }
            }
        }
    }
}
