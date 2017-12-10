using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using EmpyrionModApi;

namespace StructureOwnershipMod
{
    public class SaveState
    {
        public Dictionary<int, HashSet<int>> FactionIdToEntityIds { get; set; }
        public Dictionary<int, int> EntityIdToFactionId { get; set; }

        public Dictionary<int, ItemStacks> FactionIdToRewards { get; set; }

        public SaveState()
        {
            FactionIdToEntityIds = new Dictionary<int, HashSet<int>>();
            EntityIdToFactionId = new Dictionary<int, int>();
            FactionIdToRewards = new Dictionary<int, ItemStacks>();
        }

        public static SaveState Load(String filePath)
        {
            return EmpyrionModApi.Helpers.LoadFromYamlOrDefault<SaveState>(filePath);
        }

        public void Save(String filePath)
        {
            EmpyrionModApi.Helpers.SaveAsYaml(filePath, this);
        }
    }
}
