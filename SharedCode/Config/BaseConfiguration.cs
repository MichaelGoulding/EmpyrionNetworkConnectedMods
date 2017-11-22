using System;
using System.IO;
using YamlDotNet.Serialization;

namespace SharedCode
{
    public class BaseConfiguration
    {
        public string GameServerIp { get; set; }
        public int GameServerApiPort { get; set; }

        public int PlayerUpdateIntervalInSeconds { get; set; }

        public BaseConfiguration()
        {
            GameServerIp = "127.0.0.1";
            GameServerApiPort = 12345;
            PlayerUpdateIntervalInSeconds = 5;
        }

        public static T GetConfiguration<T>(String filePath) where T : BaseConfiguration
        {
            using (var input = File.OpenText(filePath))
            {
                return (new Deserializer()).Deserialize<T>(input);
            }
        }
    }
}
