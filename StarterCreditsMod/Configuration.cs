using EmpyrionModApi;
using System.Collections.Generic;

namespace StarterCreditsMod
{
    public class Configuration
    {
        public string StarterCreditsCommand { get; set; }
        public uint AmountOfCredits { get; set; }
        public ExpLevel MinimumLevelNeeded { get; set; }


        public Configuration()
        {
            StarterCreditsCommand = "cb:startercredits";
            AmountOfCredits = 1;
            MinimumLevelNeeded = ExpLevel.L1;
        }
    }
}
