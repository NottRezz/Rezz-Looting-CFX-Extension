using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Debug = CitizenFX.Core.Debug;

namespace Rezz_Looting_Client
{
    public class Main : BaseScript
    {
        private Config config;

        public int CurrentZoneId = 0;
        public LootArea CurrentZoneData;

        public Main()
        {
            string json = API.LoadResourceFile(API.GetCurrentResourceName(), "config.json");
            config = JsonConvert.DeserializeObject<Config>(json);

            MainLoop();
        }

        private async void MainLoop()
        {
            while (true)
            {
                await BaseScript.Delay(5000);

                int player = API.PlayerPedId();
                Vector3 playerPos = API.GetEntityCoords(player, false);

                if (CurrentZoneId == 0)
                {
                    foreach (var entry in config.LootAreas)
                    {
                        var zoneId = entry.Key;
                        var zoneData = entry.Value;

                        float dist = Vector3.Distance(zoneData.ZoneCoords, playerPos);

                        if (dist <= zoneData.Radius)
                        {
                            CurrentZoneId = zoneId;

                            Debug.WriteLine($"Entered zone {zoneId}");
                            break;
                        }
                    }
                }
                else
                {

                }
            }
        }
    }
}
