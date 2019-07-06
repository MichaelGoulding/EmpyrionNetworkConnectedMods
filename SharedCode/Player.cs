﻿using EmpyrionModApi.ExtensionMethods;
using System.Collections.Generic;
using System.Numerics;
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

        public int ClientId { get; private set; }

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

        public override async Task<WorldPosition> GetCurrentPosition()
        {
            var pInfo = await _gameServerConnection.SendRequest<Eleon.Modding.PlayerInfo>(Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(EntityId));

            UpdateInfo(pInfo);

            return Position;
        }

        public override async Task ChangePlayfield( WorldPosition newWorldPosition)
        {
            var playerPosition = await GetCurrentPosition();

            if (playerPosition.playfield == newWorldPosition.playfield)
            {
                await Teleport(newWorldPosition.position, newWorldPosition.rotation);
            }
            else
            {
                await _gameServerConnection.SendRequest(
                    Eleon.Modding.CmdId.Request_Player_ChangePlayerfield,
                    new Eleon.Modding.IdPlayfieldPositionRotation(
                        EntityId,
                        newWorldPosition.playfield.Name,
                        newWorldPosition.position.ToPVector3(),
                        newWorldPosition.rotation.ToPVector3()));
            }
        }

        //Request_Player_SetPlayerInfo = 34,

        // returns button id pressed (0-based index)
        public async Task<int> ShowDialog(string msg, string posButtonText = "Ok", string negButtonText = null)
        {
            Eleon.Modding.IdAndIntValue idAndValue = 
                await _gameServerConnection.SendRequest<Eleon.Modding.IdAndIntValue> (
                    Eleon.Modding.CmdId.Request_ShowDialog_SinglePlayer,
                    new Eleon.Modding.DialogBoxData
                    {
                        Id = EntityId,
                        MsgText = msg,
                        PosButtonText = posButtonText,
                        NegButtonText = negButtonText
                    });

            return idAndValue.Value;
        }

        public async Task RefreshInfo()
        {
            var pInfo = await _gameServerConnection.SendRequest<Eleon.Modding.PlayerInfo>(Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(EntityId));

            UpdateInfo(pInfo);
        }

        public async Task<double> GetCreditBalance()
        {
            var pInfo = await _gameServerConnection.SendRequest<Eleon.Modding.PlayerInfo>(Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(EntityId));

            UpdateInfo(pInfo);

            return pInfo.credits;
        }

        public async Task<int> GetExperiencePoints()
        {
            var pInfo = await _gameServerConnection.SendRequest<Eleon.Modding.PlayerInfo>(Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(EntityId));

            UpdateInfo(pInfo);

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

        public async Task SetExperiencePoints(int newValue)
        {
            Eleon.Modding.PlayerInfoSet pInfo = new Eleon.Modding.PlayerInfoSet();
            pInfo.entityId = EntityId;

            pInfo.experiencePoints = newValue;

            await _gameServerConnection.SendRequest<Eleon.Modding.PlayerInfo>(Eleon.Modding.CmdId.Request_Player_SetPlayerInfo, pInfo);
            _gameServerConnection.DebugOutput("Changed experiencePoints of {0} to: {1}", this, newValue);
        }

        public async Task ChangeExperiencePoints( int delta )
        {
            int expPoints = await GetExperiencePoints();

            await SetExperiencePoints(expPoints + delta);
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
            _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_InGameMessage_SinglePlayer,
                new Eleon.Modding.IdMsgPrio(EntityId, msg, (byte)priority, time));

            // Until the server is fixed, we can't wait for an answer.
            return Task.CompletedTask;
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

        public Task<Eleon.Modding.Inventory> GetInventory()
        {
            return _gameServerConnection.SendRequest<Eleon.Modding.Inventory>(Eleon.Modding.CmdId.Request_Player_GetInventory, new Eleon.Modding.Id(EntityId));
        }

        public Task<Eleon.Modding.Inventory> SetInventory(ItemStacks toolbelt, ItemStacks bag)
        {
            var newInventory = new Eleon.Modding.Inventory(EntityId, toolbelt.ToEleonList().ToArray(), bag.ToEleonList().ToArray());

            return _gameServerConnection.SendRequest<Eleon.Modding.Inventory>(Eleon.Modding.CmdId.Request_Player_SetInventory, newInventory);
        }

        public Task<Eleon.Modding.Inventory> GetAndRemoveInventory(ItemStacks toolbelt, ItemStacks bag)
        {
            var newInventory = new Eleon.Modding.Inventory(EntityId, toolbelt.ToEleonList().ToArray(), bag.ToEleonList().ToArray());

            return _gameServerConnection.SendRequest<Eleon.Modding.Inventory>(Eleon.Modding.CmdId.Request_Player_GetAndRemoveInventory, newInventory);
        }

        public Task AddMarker(string name, Vector3 position, uint expireTimeInSeconds = 0)
        {
            return AddWaypointMarkerInternal("", name, position, expireTimeInSeconds);
        }

        public Task AddWaypointMarker(string name, Vector3 position, uint expireTimeInSeconds = 0)
        {
            return AddWaypointMarkerInternal("w", name, position, expireTimeInSeconds);
        }

        public Task AddDestoryOnReachWaypointMarker(string name, Vector3 position, uint expireTimeInSeconds = 0)
        {
            return AddWaypointMarkerInternal("wd", name, position, expireTimeInSeconds);
        }

        private Task AddWaypointMarkerInternal(string typeString, string name, Vector3 position, uint expireTimeInSeconds)
        {
            string command = string.Format("remoteex cl={0} marker add name={1} pos={2},{3},{4} {5} {6}",
                this.ClientId,
                name,
                position.X,
                position.Y,
                position.Z,
                typeString,
                (expireTimeInSeconds > 0) ? string.Format("expire={0}", expireTimeInSeconds) : ""
                );

            return _gameServerConnection.RequestConsoleCommand(command);
        }

        #region Internal Methods

        internal Player(GameServerConnection gameServerConnection, Eleon.Modding.PlayerInfo pInfo)
            : base(gameServerConnection, pInfo.entityId, EntityType.Player, pInfo.playerName)
        {
            UpdateInfo(pInfo);
        }

        internal void UpdateInfo(Eleon.Modding.PlayerInfo pInfo)
        {
            var newFaction = _gameServerConnection.GetFaction(pInfo.factionId);
            var newplayField = _gameServerConnection.GetPlayfield(pInfo.playfield);

            lock (this)
            {
                System.Diagnostics.Debug.Assert(EntityId == pInfo.entityId);
                this.SteamId = pInfo.steamId;
                this.ClientId = pInfo.clientId;
                this.FactionGroupId = pInfo.factionGroup;
                this.Origin = pInfo.origin;
                this.Position = new WorldPosition { playfield = newplayField, position = pInfo.pos.ToVector3() };
                this.MemberOfFaction = newFaction;
                this.BpResourcesInFactory = pInfo.bpResourcesInFactory;
                _permission = pInfo.permission;
            }
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
