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
        private Dictionary<int, LootArea> LootAreas = new Dictionary<int, LootArea>
        {
            {1, new LootArea(1, new Vector3(-5647.97f, -3417.02f, -22.63f), 5, "Food", 5, 50)},
        };

        private Dictionary<int, Loot> LootDictionary = new Dictionary<int, Loot>();

        private int MaxLoot = 100;

        private Random rng = new Random();

        private int nextLootId = 1;

        public Main()
        {
            InitializeLoot();
        }

        private void InitializeLoot()
        {
            foreach (var entry in LootAreas)
            {
                var area = entry.Value;

                for (int i = 0; i < area.maxLoot; i++)
                {
                    if (LootDictionary.Count >= MaxLoot)
                        return;

                    if (rng.Next(1, 101) <= area.spawnChance)
                    {
                        int lootId = nextLootId++;

                        // Loot loot = new Loot(...);
                        // LootDictionary.Add(lootId, loot);

                        Debug.WriteLine($"Spawned loot {lootId} in area {area.zoneId}");
                    }
                }
            }
        }
    }
}
