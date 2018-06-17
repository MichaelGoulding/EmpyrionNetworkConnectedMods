using EmpyrionModApi;
using System.Collections.Generic;

namespace ServerPlaytimeRewardsMod
{
    public class Configuration
    {
        public double XpRewardPeriodInMinutes { get; set; }

        public int XpPerPeriod { get; set; }

        public Configuration()
        {
            XpRewardPeriodInMinutes = 1.0;
            XpPerPeriod = 100;
        }
    }
}
