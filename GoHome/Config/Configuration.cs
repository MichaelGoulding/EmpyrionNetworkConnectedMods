using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace GoHome.Config
{
    public class Configuration
    {
        public string GameServerIp { get; set; }
        public int GameServerApiPort { get; set; }

        public class Vector3
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public static implicit operator Eleon.Modding.PVector3(Vector3 v)
            {
                return new Eleon.Modding.PVector3(v.X, v.Y, v.Z);
            }
        }

        public class FactionHomeWorldData
        {
            public string Playfield { get; set; }

            public Vector3 Location { get; set; }

            public Vector3 Rotation { get; set; }
        }

        public Dictionary<int, FactionHomeWorldData> FactionHomeWorlds { get; set; }

        public Configuration()
        {
            GameServerIp = "127.0.0.1";
            GameServerApiPort = 12345;
        }

        public static Configuration GetConfiguration(String filePath)
        {
            var input = File.OpenText(filePath);

            var deserializer = new Deserializer();

            return deserializer.Deserialize<Configuration>(input);
        }
    }
}
