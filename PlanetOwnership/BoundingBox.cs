using System;
using System.Collections.Generic;

namespace PlanetOwnership
{
    class BoundingBox
    {
        public class AreaEventArgs : EventArgs
        {
            public BoundingBox AreaBeingWatched { get; set; }

        }

        public delegate void PlayerAreaEvent(Player player, AreaEventArgs e);

        public event PlayerAreaEvent PlayerEnteredArea;

        public event PlayerAreaEvent PlayerLeftArea;

        public List<Player> PlayersInArea { get; private set; }

        public BoundingBox()
        {
            PlayersInArea = new List<Player>();
            _playfield = new Playfield("Akua2");
            _rect = new Rect3(new Vector3(0, 0, 0), new Vector3(100, 100, 100));
        }

        public void OnPlayerUpdate(Player player)
        {
            // is player in area?
            bool playerInArea = ((player.Position.playfield == _playfield) && _rect.Contains(player.Position.position));

            if (playerInArea)
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

        private Playfield   _playfield;
        private Rect3       _rect;
    }
}
