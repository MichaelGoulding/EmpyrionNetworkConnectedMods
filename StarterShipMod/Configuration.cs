using EmpyrionModApi;
using System.Collections.Generic;

namespace StarterShipMod
{
    public class Configuration
    {
        public string StarterShipCommand { get; set; }
        public string BlueprintName { get; set; }
        public Entity.EntityType EntityType { get; set; }
        public string ShipNameFormat { get; set; }

        public ExpLevel MinimumLevelNeeded { get; set; }


        public Configuration()
        {
            StarterShipCommand = "cb:startership";
            BlueprintName = "SV_Prefab_Tier1";
            EntityType = Entity.EntityType.SV;
            ShipNameFormat = "{0}'s Starter Ship";
            MinimumLevelNeeded = ExpLevel.L1;
        }
    }
}
