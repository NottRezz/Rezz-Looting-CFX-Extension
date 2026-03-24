using Rezz_Looting_Server;
using System.Collections.Generic;

internal class Config
{
    public Dictionary<int, LootArea> LootAreas { get; set; } = new Dictionary<int, LootArea>();
    public Dictionary<string, List<LootDefinition>> LootTablesByType { get; set; } = new Dictionary<string, List<LootDefinition>>();
    public int MaxLoot { get; set; }
}