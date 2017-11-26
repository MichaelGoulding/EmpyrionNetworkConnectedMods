using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharedCode
{
    public class Player : IEquatable<Player>
    {
        public WorldPosition Position { get; private set; }

        public Faction MemberOfFaction { get; private set; }

        public Dictionary<int, float> BpResourcesInFactory { get; private set; }

        public bool IsPrivileged
        {
            get
            {
                // Player = 0, GameMaster = 3, Moderator = 6, Admin = 9 
                return (_permission >= 3) && (_permission <= 9);
            }
        }

        public Task AddCredits(double amount)
        {
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_Player_AddCredits,
                new Eleon.Modding.IdCredits(_entityId, amount));
        }

        public Task ChangePlayerfield( WorldPosition newWorldPosition)
        {
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_Player_ChangePlayerfield,
                new Eleon.Modding.IdPlayfieldPositionRotation(_entityId, newWorldPosition.playfield.Name, newWorldPosition.position, newWorldPosition.rotation));
        }

        public Task SendAlarmMessage(string format, params object[] args)
        {
            return SendMessage(MessagePriority.Alarm, 10, format, args);
        }

        public Task SendAlertMessage(string format, params object[] args)
        {
            return SendMessage(MessagePriority.Alert, 10, format, args);
        }

        public Task SendAttentionMessage(string format, params object[] args)
        {
            return SendMessage(MessagePriority.Attention, 10, format, args);
        }

        public Task SendMessage(MessagePriority priority, float time, string format, params object[] args)
        {
            string msg = string.Format(format, args);
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_InGameMessage_SinglePlayer,
                new Eleon.Modding.IdMsgPrio(_entityId, msg, (byte)priority, time));
        }

        #region Common overloads

        public override string ToString()
        {
            return _entityId.ToString();
        }

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

        #endregion

        #region Internal Methods

        internal Player(GameServerConnection gameServerConnection, Eleon.Modding.PlayerInfo pInfo)
        {
            _entityId = pInfo.entityId;
            _gameServerConnection = gameServerConnection;
            UpdateInfo(pInfo, _gameServerConnection.GetPlayfield(pInfo.playfield));
        }

        internal void UpdateInfo(Eleon.Modding.PlayerInfo pInfo, Playfield playfield)
        {
            System.Diagnostics.Debug.Assert(_entityId == pInfo.entityId);
            this.Position = new WorldPosition { playfield = playfield, position = new Vector3(pInfo.pos) };
            this.MemberOfFaction = new Faction(pInfo.factionId);
            this.BpResourcesInFactory = pInfo.bpResourcesInFactory;
            _permission = pInfo.permission;
        }

        internal void UpdateInfo(Playfield playfield)
        {
            this.Position = new WorldPosition(playfield, this.Position.position, this.Position.rotation);
        }

        internal int EntityId
        {
            get
            {
                return _entityId;
            }
        }

        #endregion

        #region Private methods

        #endregion

        #region Private Data

        private GameServerConnection _gameServerConnection;
        private int _entityId;
        private int _permission;

        #endregion
    }

    // onPlayerEnteredPlayfield

}
