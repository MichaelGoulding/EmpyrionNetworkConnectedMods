using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using YamlDotNet.Serialization;

namespace PlanetOwnership.Config
{
    public class Configuration
    {
        public string GameServerIp { get; set; }
        public int GameServerApiPort { get; set; }

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

        public int EntityToCapture { get; set; }

        public double CaptureRewardMinutes { get; set; }

        public Configuration()
        {
            GameServerIp = "127.0.0.1";
            GameServerApiPort = 12345;

            CaptureRewardMinutes = 0.5;
        }

        public static Configuration GetConfiguration(String filePath)
        {
            var input = File.OpenText(filePath);

            var deserializer = new Deserializer();

            return deserializer.Deserialize<Configuration>(input);
        }
    }
}
