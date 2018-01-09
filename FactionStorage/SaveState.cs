using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using EmpyrionModApi;

namespace FactionStorageMod
{
    public class SaveState
    {
        public Dictionary<int, ItemStacks> FactionIdToItemStacks { get; set; }

        public SaveState()
        {
            FactionIdToItemStacks = new Dictionary<int, ItemStacks>();
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
