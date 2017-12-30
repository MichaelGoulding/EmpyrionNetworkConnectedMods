using EmpyrionModApi;
using System.Collections.Generic;

namespace ShipBuyingMod
{
    public class Configuration
    {
        public string BuyShipCommand { get; set; }

        public class ShipSeller
        {
            public string Name { get; set; }

            public byte Origin { get; set; }

            public BoundingBoxInfo BoundingBox { get; set; }

            public class ShipInfo
            {
                public string DisplayName { get; set; }

                public string BlueprintName { get; set; }

                public string ShipNameFormat { get; set; }

                public double Price { get; set; }

                public BoundingBoxInfo.Vector3 SpawnLocation { get; set; }
            }

            public List<ShipInfo> ShipsForSale { get; set; }
        }

        public List<ShipSeller> ShipSellers { get; set; }

        public Configuration()
        {
            BuyShipCommand = "/buyship";
            ShipSellers = new List<ShipSeller>();
        }
    }
}
