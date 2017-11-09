using System;
using System.Collections.Generic;

namespace PlanetOwnership
{
    class Player : IEquatable<Player>
    {
        private int _entityId;

        public Player(Eleon.Modding.PlayerInfo pInfo)
        {
            _entityId = pInfo.entityId;
            this.UpdateInfo(pInfo);
        }

        public void UpdateInfo(Eleon.Modding.PlayerInfo pInfo)
        {
            System.Diagnostics.Debug.Assert(_entityId == pInfo.entityId);
            this.Position = new WorldPosition { playfield = new Playfield(pInfo.playfield), position = new Vector3(pInfo.pos) };
            this.MemberOfFaction = new Faction(pInfo.factionId);
            this.BpResourcesInFactory = pInfo.bpResourcesInFactory;
        }

        public WorldPosition Position { get; set; }

        public Faction MemberOfFaction { get; set; }

        public Dictionary<int, float> BpResourcesInFactory { get; set; }


        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            Player p = obj as Player;
            if (p == null)
            {
                return false;
            }

            return (_entityId == p._entityId);
        }

        public bool Equals(Player other)
        {
            return other != null &&
                   _entityId == other._entityId;
        }

        public override int GetHashCode()
        {
            return -1485059848 + _entityId.GetHashCode();
        }

        public static bool operator ==(Player player1, Player player2)
        {
            return EqualityComparer<Player>.Default.Equals(player1, player2);
        }

        public static bool operator !=(Player player1, Player player2)
        {
            return !(player1 == player2);
        }
    }

    // onPlayerEnteredPlayfield

}
