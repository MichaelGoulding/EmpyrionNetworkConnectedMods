using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using EmpyrionModApi;

namespace StarterCreditsMod
{
    public class SaveState
    {
        public HashSet<int> IdsOfThoseWhoGotTheirCredits { get; set; }

        public SaveState()
        {
            IdsOfThoseWhoGotTheirCredits = new HashSet<int>();
        }

        public static SaveState Load(String filePath)
        {
            return EmpyrionModApi.Helpers.LoadFromYamlOrDefault<SaveState>(filePath);
        }

        public void Save(String filePath)
        {
            EmpyrionModApi.Helpers.SaveAsYaml(filePath, this);
        }

        public bool HasGotStarterCredits(Player player)
        {
            return IdsOfThoseWhoGotTheirCredits.Contains(player.EntityId);
        }

        internal void MarkGotStarterCredits(Player player)
        {
            IdsOfThoseWhoGotTheirCredits.Add(player.EntityId);
        }
    }
}
