using System.Collections.Generic;

namespace SellToServerMod
{
    public class Configuration
    {
        public class SellLocation
        {
            public EmpyrionModApi.BoundingBoxInfo BoundingBox { get; set; }

            public Dictionary<int, double> ItemIdToUnitPrice { get; set; }

            public double DefaultPrice { get; set; }
        }

        public List<SellLocation> SellLocations { get; set; }
    }
}
