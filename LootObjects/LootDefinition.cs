namespace Rezz_Looting_Server
{
    internal class LootDefinition
    {
        public string LootName { get; set; }
        public string LootLabel { get; set; }
        public string LootType { get; set; }
        public int MinAmount { get; set; }
        public int MaxAmount { get; set; }
        public int MinTier { get; set; }
        public int MaxTier { get; set; }
        public string Loot3dModel { get; set; }
        // Remove the constructor entirely, Newtonsoft handles it

        public LootDefinition(string lootName, string lootLabel, string lootType, int minAmount, int maxAmount, int minTier, int maxTier, string Loot3dModel)
        {
            LootName = lootName;
            LootLabel = lootLabel;
            LootType = lootType;
            MinAmount = minAmount;
            MaxAmount = maxAmount;
            MinTier = minTier;
            MaxTier = maxTier;
            this.Loot3dModel = Loot3dModel;
        }
    }
}