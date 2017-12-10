using EmpyrionModApi;
using System.Collections.Generic;

namespace StructureOwnershipMod
{
    public class Configuration
    {
        public Dictionary<int, ItemStacks> EntityIdToRewards { get; set; }

        public double CaptureRewardPeriodInMinutes { get; set; }

        public Configuration()
        {
            CaptureRewardPeriodInMinutes = 0.5;
        }
    }
}
