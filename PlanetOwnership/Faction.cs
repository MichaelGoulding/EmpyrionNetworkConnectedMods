namespace PlanetOwnership
{
    class Faction
    {
        // TODO: implement equals
        private int factionId;

        public Faction(int factionId)
        {
            this.factionId = factionId;
        }

        public int Id
        {
            get
            {
                return factionId;
            }
        }
    }
}
