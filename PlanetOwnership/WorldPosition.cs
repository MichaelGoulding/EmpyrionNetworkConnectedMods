namespace PlanetOwnership
{
    struct WorldPosition
    {
        public Playfield playfield;
        public Vector3 position;

        public WorldPosition(Playfield playfield, Vector3 position)
        {
            this.playfield = playfield;
            this.position = position;
        }
    }
}
