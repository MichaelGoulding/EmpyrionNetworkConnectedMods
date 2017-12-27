using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using EmpyrionModApi;

namespace StarterShipMod
{
    public class SaveState
    {
        public HashSet<int> IdsOfThoseWhoGotTheirShips { get; set; }

        public SaveState()
        {
            IdsOfThoseWhoGotTheirShips = new HashSet<int>();
        }

        public static SaveState Load(String filePath)
        {
            return EmpyrionModApi.Helpers.LoadFromYamlOrDefault<SaveState>(filePath);
        }

        public void Save(String filePath)
        {
            EmpyrionModApi.Helpers.SaveAsYaml(filePath, this);
        }

        public bool HasGotStarterShip(Player player)
        {
            return IdsOfThoseWhoGotTheirShips.Contains(player.EntityId);
        }

        internal void MarkGotStarterShip(Player player)
        {
            IdsOfThoseWhoGotTheirShips.Add(player.EntityId);
        }
    }
}
