using System.Collections.Generic;

namespace FactionPlayfieldKickerMod
{
    public class Configuration
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
