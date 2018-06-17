using EmpyrionModApi;
using System.Collections.Generic;

namespace ServerPlaytimeRewardsMod
{
    public class Configuration
    {
        public WorldPositionInfo JailLocation { get; set; }

        public WorldPositionInfo JailExitLocation { get; set; }

        public class NoKillZone
        {
            public string Name { get; set; }

            public BoundingBoxInfo BoundingBox { get; set; }
        }

        public List<NoKillZone> NoKillZones { get; set; }

        public Configuration()
        {
            NoKillZones = new List<NoKillZone>();
        }
    }
}
