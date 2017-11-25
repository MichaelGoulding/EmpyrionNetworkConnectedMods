using System.Collections.Generic;

namespace PlayfieldStructureRegenMod
{
    public class Configuration
    {
        public class PlayfieldEntityRegenData
        {
            public List<int> StructuresIds { get; set; }
        }

        public Dictionary<string, PlayfieldEntityRegenData> PlayfieldsToRegenerate { get; set; }
    }
}
