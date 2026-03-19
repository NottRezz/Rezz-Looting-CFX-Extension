using CitizenFX.Core;
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

        private Dictionary<int, Loot> LootDictionary = new Dictionary<int, Loot>(); // MAIN LOOT DICT

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

                for (int i = 0; i < area.MaxLoot; i++)
                {
                    if (LootDictionary.Count >= config.MaxLoot)
                        return;

                    if (rng.Next(1, 101) <= area.SpawnChance)
                    {
                        int lootId = nextLootId++;

                        // Loot loot = new Loot(...);
                        // LootDictionary.Add(lootId, loot);

                        Debug.WriteLine($"Spawned loot {lootId} in area {area.ZoneId}");
                    }
                }
            }
        }
    }
}
