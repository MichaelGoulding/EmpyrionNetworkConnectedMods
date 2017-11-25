using System.Collections.Generic;

namespace StructureOwnershipMod
{
    public class Configuration
    {
        public class EntityCaptureItem
        {
            public int EntityId { get; set; }

            public double CaptureRewardMinutes { get; set; }

            public class RewardItemStack
            {
                public int Id { get; set; }
                public int Amount { get; set; }
            }

            public List<RewardItemStack> RewardItemStacks { get; set; }
        };

        public List<EntityCaptureItem> EntityCaptureItems { get; set; }

        public double CaptureRewardMinutes { get; set; }

        public Configuration()
        {
            CaptureRewardMinutes = 0.5;
        }
    }
}
