using System.Collections.Generic;

namespace PlayfieldStructureRegenMod
{
    public class Configuration
    {
        public class PlayfieldEntityRegenData
        {
            public List<int> StructuresIds { get; set; }

            public bool RegenerateAllAsteroids { get; set; }

            public PlayfieldEntityRegenData()
            {
                StructuresIds = new List<int>();
            }
        }

        public Dictionary<string, PlayfieldEntityRegenData> PlayfieldsToRegenerate { get; set; }
    }
}
