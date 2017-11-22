using System.Collections.Generic;

namespace FactionPlayfieldKicker
{
    public class Configuration : SharedCode.BaseConfiguration
    {
        public string BootMessage { get; set; }

        public Dictionary<string, int> FactionHomeWorlds { get; set; }

        public Configuration()
        {
            // set defaults
            BootMessage = "You are not allowed to enter this faction's playfield.";
        }
    }
}
