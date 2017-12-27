using EmpyrionModApi.ExtensionMethods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmpyrionModApi
{
    public class Player : Entity
    {
        public bool IsPrivileged
        {
            get
            {
                // Player = 0, GameMaster = 3, Moderator = 6, Admin = 9 
                return (_permission >= 3) && (_permission <= 9);
            }
        }

        public Dictionary<int, float> BpResourcesInFactory { get; protected set; }

        public string SteamId { get; private set; }

        public Task AddCredits(double amount)
        {
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_Player_AddCredits,
                new Eleon.Modding.IdCredits(EntityId, amount));
        }

        //Request_Player_Credits = 25,
        //Request_Player_SetCredits = 26,

        //Request_Player_AddItem = 24,

        public Task FinishBlueprint()
        {
            return _gameServerConnection.SendRequest(Eleon.Modding.CmdId.Request_Blueprint_Finish, new Eleon.Modding.Id(EntityId));
        }


        public Task AddBlueprintResources(List<Eleon.Modding.ItemStack> itemStacks)
        {
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_Blueprint_Resources,
                new Eleon.Modding.BlueprintResources(EntityId, itemStacks, false));
        }

        public Task SetBlueprintResources(List<Eleon.Modding.ItemStack> itemStacks)
        {
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_Blueprint_Resources,
                new Eleon.Modding.BlueprintResources(EntityId, itemStacks, true));
        }

        public Task ChangePlayerfield( WorldPosition newWorldPosition)
        {
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_Player_ChangePlayerfield,
                new Eleon.Modding.IdPlayfieldPositionRotation(
                    EntityId,
                    newWorldPosition.playfield.Name,
                    newWorldPosition.position.ToPVector3(),
                    newWorldPosition.rotation.ToPVector3()));
        }

        //Request_Player_SetPlayerInfo = 34,

        public Task ShowDialog(string msg, MessagePriority priority, float time=10)
        {
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_ShowDialog_SinglePlayer,
                new Eleon.Modding.IdMsgPrio(EntityId, msg, (byte)priority, time));
        }

        public Task SendChatMessage(string format, params object[] args)
        {
            string msg = string.Format(format, args);
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_ConsoleCommand,
                new Eleon.Modding.PString($"SAY p:{EntityId} '{msg}'"));
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
                new Eleon.Modding.IdMsgPrio(EntityId, msg, (byte)priority, time));
        }

        public Task<Eleon.Modding.ItemExchangeInfo> DoItemExchange(string title, string description, string buttonText,ItemStacks items = null)
        {
            return DoItemExchange(title, description, buttonText, items?.ToEleonArray());
        }

        public Task<Eleon.Modding.ItemExchangeInfo> DoItemExchange(string title, string description, string buttonText, Eleon.Modding.ItemStack[] items)
        {
            var data = new Eleon.Modding.ItemExchangeInfo();
            data.id = EntityId;
            data.title = title;
            data.desc = description;
            data.buttonText = buttonText;
            data.items = items;

            return _gameServerConnection.SendRequest<Eleon.Modding.ItemExchangeInfo>(Eleon.Modding.CmdId.Request_Player_ItemExchange, data);
        }

        #region Internal Methods

        internal Player(GameServerConnection gameServerConnection, Eleon.Modding.PlayerInfo pInfo)
            : base(gameServerConnection, pInfo.entityId, EntityType.Player, pInfo.playerName)
        {
            UpdateInfo(pInfo, _gameServerConnection.GetPlayfield(pInfo.playfield));
        }

        internal void UpdateInfo(Eleon.Modding.PlayerInfo pInfo, Playfield playfield)
        {
            System.Diagnostics.Debug.Assert(EntityId == pInfo.entityId);
            this.SteamId = pInfo.steamId;
            this.FactionGroupId = pInfo.factionGroup;
            this.Position = new WorldPosition { playfield = playfield, position = pInfo.pos.ToVector3() };
            this.MemberOfFaction = _gameServerConnection.GetFaction(pInfo.factionId);
            this.BpResourcesInFactory = pInfo.bpResourcesInFactory;
            _permission = pInfo.permission;
        }

        #endregion

        #region Private methods

        #endregion

        #region Private Data

        private int _permission;

        #endregion
    }

    // onPlayerEnteredPlayfield

}
