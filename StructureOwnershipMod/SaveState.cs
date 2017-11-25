using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace StructureOwnershipMod
{
    public class SaveState
    {
        public Dictionary<int, HashSet<int>> FactionIdToEntityIds { get; set; }
        public Dictionary<int, int> EntityIdToFactionId { get; set; }

        public SaveState()
        {
            FactionIdToEntityIds = new Dictionary<int, HashSet<int>>();
            EntityIdToFactionId = new Dictionary<int, int>();
        }

        public static SaveState Load(String filePath)
        {
            return SharedCode.Helpers.LoadFromYamlOrDefault<SaveState>(filePath);
        }

        public void Save(String filePath)
        {
            SharedCode.Helpers.SaveAsYaml(filePath, this);
        }
    }
}
