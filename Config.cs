using CitizenFX.Core;
using Rezz_Looting_Server.LootObjects;
using System.Collections.Generic;

namespace Rezz_Looting_Server
{
    internal class Config
    {
        public Dictionary<int, LootArea> LootAreas { get; set; }
        public Dictionary<string, List<LootDefinition>> LootTablesByType { get; set; }
        public int MaxLoot { get; set; }

        public Config()
        {
            MaxLoot = 100;

            // =========================
            // LOOT AREAS (WHERE LOOT SPAWNS)
            // =========================
            LootAreas = new Dictionary<int, LootArea>
            {
                {
                    0,
                    new LootArea(
                        0,
                        new Vector3(-5647.97f, -3417.02f, -22.63f),

                        new Dictionary<int, LootAreaSpawnZones>
                        {
                            {0, new LootAreaSpawnZones(1, 0, new Vector3(-5647.97f, -3417.02f, -22.63f), "NONE") },
                            {1, new LootAreaSpawnZones(1, 1, new Vector3(-5647.97f, -3417.02f, -22.63f), "NONE") },
                            {2, new LootAreaSpawnZones(1, 2, new Vector3(-5647.97f, -3417.02f, -22.63f), "NONE") }

                        },

                        5,"Food",5,100,1
                    )
                },

                {
                    1,
                    new LootArea(
                        1,
                        new Vector3(-5655.00f, -3425.00f, -22.63f),

                        new Dictionary<int, LootAreaSpawnZones>
                        {
                            {0, new LootAreaSpawnZones(2, 0, new Vector3(-5647.97f, -3417.02f, -22.63f), "NONE") },
                        },

                        5,"Medical", 4,100,2
                    )
                },

                {
                    2,
                    new LootArea(
                        2,
                        new Vector3(-5665.00f, -3430.00f, -22.63f),

                        new Dictionary<int, LootAreaSpawnZones>
                        {
                            { 0, new LootAreaSpawnZones(3, 0, new Vector3(-5647.97f, -3417.02f, -22.63f), "NONE") },
                            { 1, new LootAreaSpawnZones(3, 1, new Vector3(-5647.97f, -3417.02f, -22.63f), "NONE") },
                            { 2, new LootAreaSpawnZones(3, 2, new Vector3(-5647.97f, -3417.02f, -22.63f), "NONE") },
                        },

                        5, "Military",3,30,3
                    )
                }
            };

            // =========================
            // LOOT TABLES (WHAT CAN SPAWN)
            // GROUPED BY TYPE
            // =========================
            LootTablesByType = new Dictionary<string, List<LootDefinition>>
            {
                ["Food"] = new List<LootDefinition>
                {
                    new LootDefinition("beans", "Canned Beans", "Food", 1, 2, 1, 2),
                    new LootDefinition("water", "Water Bottle", "Food", 1, 1, 1, 3),
                    new LootDefinition("chips", "Bag of Chips", "Food", 1, 2, 1, 1)
                },

                ["Medical"] = new List<LootDefinition>
                {
                    new LootDefinition("bandage", "Bandage", "Medical", 1, 3, 1, 3),
                    new LootDefinition("medkit", "Med Kit", "Medical", 1, 1, 2, 3)
                },

                ["Military"] = new List<LootDefinition>
                {
                    new LootDefinition("pistol_ammo", "Pistol Ammo", "Military", 6, 18, 2, 3),
                    new LootDefinition("rifle_ammo", "Rifle Ammo", "Military", 10, 30, 3, 4)
                }
            };
        }
    }
}