using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using YamlDotNet.Serialization;

namespace PlanetOwnership.Config
{
    public class SaveState
    {
        public int OwningFactionId { get; set; }


        public Dictionary<int, List<int>> FactionIdToEntityIds { get; set; }

        public SaveState()
        {
            FactionIdToEntityIds = new Dictionary<int, List<int>>();
        }

        public static SaveState Load(String filePath)
        {
            if (File.Exists(filePath))
            {
                using (var input = File.OpenText(filePath))
                {
                    return (new Deserializer()).Deserialize<SaveState>(input);
                }
            }
            else
            {
                return new SaveState();
            }
        }

        public void Save(String filePath)
        {
            using (StreamWriter writer = File.CreateText(filePath))
            {
                var serializer = new Serializer();

                serializer.Serialize(writer, this);
            }
        }
    }
}
