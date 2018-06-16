using System;
using System.Collections.Generic;
using System.Numerics;

namespace EmpyrionModApi
{
    public class BoundingBoxInfo
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

        public class Rect3
        {
            public Vector3 min { get; set; }
            public Vector3 max { get; set; }
        }

        public Rect3 Rect { get; set; }
    }

    public class BoundingBox
    {
        public class AreaEventArgs : EventArgs
        {
            public BoundingBox AreaBeingWatched { get; set; }
        }

        public delegate void PlayerAreaEvent(Player player, AreaEventArgs e);

        public event PlayerAreaEvent PlayerEnteredArea;

        public event PlayerAreaEvent PlayerLeftArea;

        public List<Player> PlayersInArea { get; private set; }

        public BoundingBox(Playfield playfield, Rect3 rect)
        {
            PlayersInArea = new List<Player>();
            _playfield = playfield;
            _rect = rect;
        }

        public BoundingBox(IGameServerConnection gameServerConnection, BoundingBoxInfo boundingBoxInfo)
        {
            PlayersInArea = new List<Player>();
            _playfield = gameServerConnection.GetPlayfield(boundingBoxInfo.Playfield);
            _rect = new Rect3(
                new Vector3(boundingBoxInfo.Rect.min.x, boundingBoxInfo.Rect.min.y, boundingBoxInfo.Rect.min.z),
                new Vector3(boundingBoxInfo.Rect.max.x, boundingBoxInfo.Rect.max.y, boundingBoxInfo.Rect.max.z));
        }

        public void OnPlayerUpdate(Player player, WorldPosition playerPosition)
        {
            // is player in area?
            if (IsInside(playerPosition))
            {
                // if not on list, add them and update listeners
                if (!PlayersInArea.Contains(player))
                {
                    PlayersInArea.Add(player);
                    PlayerEnteredArea.Invoke(player, new AreaEventArgs { AreaBeingWatched = this });
                }
            }
            else
            {
                // if on list, remove them and update listeners
                if (PlayersInArea.Contains(player))
                {
                    PlayersInArea.Remove(player);
                    PlayerLeftArea.Invoke(player, new AreaEventArgs { AreaBeingWatched = this });
                }
            }
        }

        public bool IsInside(WorldPosition position)
        {
            return ((position.playfield == _playfield) && _rect.Contains(position.position));
        }

        private Playfield   _playfield;
        private Rect3       _rect;
    }
}
