using System.Numerics;

namespace EmpyrionModApi
{
    public class WorldPositionInfo
    {
        public string Playfield { get; set; }

        public class Vector3
        {
            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }

            public System.Numerics.Vector3 ToNumericsVector3()
            {
                return new System.Numerics.Vector3(x, y, z);
            }
        }

        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
    }

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

        public WorldPosition(IGameServerConnection gameServerConnection, WorldPositionInfo worldPositionInfo)
        {
            this.playfield = gameServerConnection.GetPlayfield(worldPositionInfo.Playfield);
            this.position = worldPositionInfo.Position.ToNumericsVector3();
            this.rotation = worldPositionInfo.Rotation.ToNumericsVector3();
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", playfield, position, rotation);
        }
    }
}
