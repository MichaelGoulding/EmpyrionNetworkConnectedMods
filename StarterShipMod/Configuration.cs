using EmpyrionModApi;
using System.Collections.Generic;

namespace StarterShipMod
{
    public class Configuration
    {
        public string StarterShipCommand { get; set; }
        public string BlueprintName { get; set; }

        public string ShipNameFormat { get; set; }


        public Configuration()
        {
            StarterShipCommand = "cb:startership";
            BlueprintName = "SV_Prefab_Tier1";
            ShipNameFormat = "{0}'s Starter Ship";
        }
    }
}
