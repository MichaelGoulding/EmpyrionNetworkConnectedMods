namespace SharedCode
{
    public struct WorldPosition
    {
        public Playfield playfield;
        public Vector3 position;
        public Vector3 rotation;

        public WorldPosition(Playfield playfield, Vector3 position, Vector3 rotation)
        {
            this.playfield = playfield;
            this.position = position;
            this.rotation = rotation;
        }
    }
}
