using System;
using System.Collections.Generic;

namespace SharedCode
{
    public class BoundingBoxInfo
    {
        public string Playfield { get; set; }

        public class Vector3
        {
            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }
        }

        public class Rect3
        {
            public Vector3 pt0 { get; set; }
            public Vector3 pt1 { get; set; }
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

        public BoundingBox(GameServerConnection gameServerConnection, BoundingBoxInfo boundingBoxInfo)
        {
            PlayersInArea = new List<Player>();
            _playfield = gameServerConnection.GetPlayfield(boundingBoxInfo.Playfield);
            _rect = new Rect3(
                new Vector3(boundingBoxInfo.Rect.pt0.x, boundingBoxInfo.Rect.pt0.y, boundingBoxInfo.Rect.pt0.z),
                new Vector3(boundingBoxInfo.Rect.pt1.x, boundingBoxInfo.Rect.pt1.y, boundingBoxInfo.Rect.pt1.z));
        }

        public void OnPlayerUpdate(Player player)
        {
            // is player in area?
            if (IsInside(player))
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

        public bool IsInside(Player player)
        {
            return ((player.Position.playfield == _playfield) && _rect.Contains(player.Position.position));
        }

        private Playfield   _playfield;
        private Rect3       _rect;
    }
}
