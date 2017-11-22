using System.Collections.Generic;

namespace SecureTrading
{
    enum ItemId
    {
        CannedVegetables = 2411,
        CannedMeat = 2412,
        AkuaBerry = 2413,
        Pizza = 2415,
    }

    public class Configuration : SharedCode.BaseConfiguration
    {
        public Dictionary<int, double> ItemIdToUnitPrice { get; set; }


        public Configuration()
        {
            ItemIdToUnitPrice = new Dictionary<int, double>();
        }
    }
}
