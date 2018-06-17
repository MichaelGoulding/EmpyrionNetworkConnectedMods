using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using EmpyrionModApi;

namespace NoKillZonesMod
{
    public class SaveState
    {
        public HashSet<int> IdsOfThosePunished { get; set; }

        public SaveState()
        {
            IdsOfThosePunished = new HashSet<int>();
        }

        public static SaveState Load(String filePath)
        {
            return EmpyrionModApi.Helpers.LoadFromYamlOrDefault<SaveState>(filePath);
        }

        public void Save(String filePath)
        {
            EmpyrionModApi.Helpers.SaveAsYaml(filePath, this);
        }

        public bool HasGottenPunished(Player player)
        {
            return IdsOfThosePunished.Contains(player.EntityId);
        }

        internal void MarkPunished(Player player)
        {
            IdsOfThosePunished.Add(player.EntityId);
        }
    }
}
