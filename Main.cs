using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Debug = CitizenFX.Core.Debug;

namespace Rezz_Looting_Client
{
    public class Main : BaseScript
    {
        private Config config;

        public int CurrentZoneId = -1;
        public LootArea CurrentZoneData;

        public Main()
        {
            string json = API.LoadResourceFile(API.GetCurrentResourceName(), "config.json");
            config = JsonConvert.DeserializeObject<Config>(json);

            EventHandlers["rezz_looting:client:RecieveLootData"] += new Action<int, string>(RecieveLoot);

            MainLoop();
        }

        private void RecieveLoot(int playerId, string LootData)
        {
            var loot = JsonConvert.DeserializeObject<Dictionary<string, Loot>>(LootData);
            foreach (var entry in loot)
            {
                var data = entry.Value;

                Debug.WriteLine($"Loot Data: {data.LootId} {data.LootName} {data.LootLabel} {data.LootType} {data.LootAmount} {data.LootEntityId}");
            }
        }

        private async void MainLoop()
        {
            while (true)
            {
                await BaseScript.Delay(5000);
                int player = API.PlayerPedId();
                Vector3 playerPos = API.GetEntityCoords(player, true,true);

                if (CurrentZoneId == -1)
                {
                    foreach (var entry in config.LootAreas)
                    {
                       var zoneId = entry.Key;
                        var zoneData = entry.Value;

                        float dist = Vector3.Distance(playerPos, new Vector3(zoneData.ZoneCoords.X, zoneData.ZoneCoords.Y, zoneData.ZoneCoords.Z));

                        if (dist <= zoneData.Radius)
                        {
                            TriggerServerEvent("rezz_loot:server:enteredZone", zoneId, true);
                            CurrentZoneId = zoneId;
                            CurrentZoneData = zoneData;
                            Debug.WriteLine($"Entered zone {zoneId}");
                            break;
                        }
                    }
                }
                else
                {
                    float dist = Vector3.Distance(new Vector3(CurrentZoneData.ZoneCoords.X, CurrentZoneData.ZoneCoords.Y, CurrentZoneData.ZoneCoords.Z), playerPos);
                    if (dist > CurrentZoneData.Radius)
                    {
                        Debug.WriteLine($"Leaving zone {CurrentZoneId}");
                        TriggerServerEvent("rezz_loot:server:enteredZone", CurrentZoneId, false);
                        CurrentZoneId = -1;
                        CurrentZoneData = null;
                    }
                }
            }
        }
    }
}