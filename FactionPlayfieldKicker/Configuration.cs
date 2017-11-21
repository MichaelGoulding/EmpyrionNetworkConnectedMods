using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace FactionPlayfieldKicker
{
    public class Configuration
    {
        public string GameServerIp { get; set; }
        public int GameServerApiPort { get; set; }

        public int PlayerUpdateIntervalInSeconds { get; set; }

        public string BootMessage { get; set; }

        public Dictionary<string, int> FactionHomeWorlds { get; set; }

        public Configuration()
        {
            // set defaults
            GameServerIp = "127.0.0.1";
            GameServerApiPort = 12345;
            PlayerUpdateIntervalInSeconds = 5;
            BootMessage = "You are not allowed to enter this faction's playfield.";
        }

        public static Configuration GetConfiguration(String filePath)
        {
            using (var input = File.OpenText(filePath) )
            {
                var deserializer = new Deserializer();
                return deserializer.Deserialize<Configuration>(input);
            }
        }
    }
}
