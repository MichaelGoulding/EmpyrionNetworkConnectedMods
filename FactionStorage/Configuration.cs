using EmpyrionModApi;
using System.Collections.Generic;

namespace FactionStorageMod
{
    public class Configuration
    {
        public string FactionStorageCommand { get; set; }

        public Configuration()
        {
            FactionStorageCommand = "/factionStorage";
        }
    }
}
