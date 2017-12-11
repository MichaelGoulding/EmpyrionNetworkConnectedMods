using System.Numerics;

namespace EmpyrionModApi
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

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", playfield, position, rotation);
        }
    }
}
