using EmpyrionModApi.ExtensionMethods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmpyrionModApi
{
    public enum ExpLevel
    {
        L1 = 0,
        L2 = 799,
        L3 = 3199,
        L4 = 7199,
        L5 = 12799,
        L6 = 20000,
        L7 = 28799,
        L8 = 39200,
        L9 = 51199,
        L10 = 64800,
        L11 = 80000,
        L12 = 96799,
        L13 = 115199,
        L14 = 135199,
        L15 = 156800,
        L16 = 180000,
        L17 = 204799,
        L18 = 231200,
        L19 = 259200,
        L20 = 288800,
        L21 = 320000,
        L22 = 352799,
        L23 = 387199,
        L24 = 423200,
        L25 = 500000,
    }

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

        public async Task RefreshInfo()
        {
            var pInfo = await _gameServerConnection.SendRequest<Eleon.Modding.PlayerInfo>(Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(EntityId));

            UpdateInfo(pInfo, this.Position.playfield);
        }

        public async Task<double> GetCreditBalance()
        {
            var pInfo = await _gameServerConnection.SendRequest<Eleon.Modding.PlayerInfo>(Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(EntityId));

            UpdateInfo(pInfo, this.Position.playfield);

            return pInfo.credits;
        }

        public async Task<int> GetExperiencePoints()
        {
            var pInfo = await _gameServerConnection.SendRequest<Eleon.Modding.PlayerInfo>(Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(EntityId));

            UpdateInfo(pInfo, this.Position.playfield);

            return pInfo.exp;
        }

        public async Task<ExpLevel> GetExperienceLevel()
        {
            ExpLevel result = ExpLevel.L1;

            int expPoints = await GetExperiencePoints();

            foreach (ExpLevel level in System.Enum.GetValues(typeof(ExpLevel)))
            {
                if (expPoints < (int)level)
                {
                    break;
                }
                else
                {
                    result = level;
                }
            }

            return result;
        }

        public Task SendChatMessage(string format, params object[] args)
        {
            string msg = format.SafeFormat(args);
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
            string msg = format.SafeFormat(args);
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
            this.Origin = pInfo.origin;
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
