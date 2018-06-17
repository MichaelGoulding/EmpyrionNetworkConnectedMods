using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using Eleon.Modding;
using EmpyrionModApi.ExtensionMethods;

namespace EmpyrionModApi
{
    public enum MessagePriority : byte
    {
        Alarm = 0,
        Alert = 1,
        Attention = 2,
    }

    public class GameServerConnection : IGameServerConnection
    {
        #region Private Data

        const int k_RequestFactionListStartingFromId = 1;

        List<string> _versionStrings = new List<string>();

        EPMConnector.Client _client = new EPMConnector.Client(12345);

        Dictionary<int, Player> _onlinePlayersInfoById = new Dictionary<int, Player>();
        Dictionary<string, Playfield> _playfieldsByName = new Dictionary<string, Playfield>();
        Dictionary<int, Faction> _factionsById = new Dictionary<int, Faction>();

        List<BoundingBox> _bboxes = new List<BoundingBox>();

        BaseConfiguration _config;
        private TraceSource _traceSource = new TraceSource("GameServerConnection");

        private RequestTracker _requestTracker = new RequestTracker();

        #endregion

        #region Public Events

        public event Action<Player> Event_Player_Connected;
        public event Action<Player> Event_Player_Disconnected;
        public event Action<Playfield, Player> Event_Player_ChangedPlayfield;
        public event Action<Player, PlayerDeathInfo> Event_PlayerDied;

        public event Action<Playfield> Event_Playfield_Loaded;
        public event Action<ChatType, string, Player> Event_ChatMessage;
        public event Action<Eleon.Modding.FactionChangeInfo> Event_Faction_Changed;

        #endregion

        #region Public Methods

        public GameServerConnection(BaseConfiguration config)
        {
            _config = config;
            _client.GameEventReceived += OnClient_GameEventReceived;
            _client.ClientMessages += (string s) => { Console.Out.WriteLine("Client_ClientMessages: {0}", s); };
        }

        public void AddVersionString(string versionString)
        {
            _versionStrings.Add(versionString);
            DebugOutput("Adding Mod: \"{0}\"", versionString);
        }

        public void Connect()
        {
            // connect to server
            _client.OnConnected += async () =>
            {
                try
                {
                    // Request various lists to start off
                    ProcessEvent_Get_Factions(await SendRequest<Eleon.Modding.FactionInfoList>(Eleon.Modding.CmdId.Request_Get_Factions, new Eleon.Modding.Id(k_RequestFactionListStartingFromId)));
                    ProcessEvent_Player_List(await SendRequest<Eleon.Modding.IdList>(Eleon.Modding.CmdId.Request_Player_List, null));
                    ProcessEvent_Playfield_List(await SendRequest<Eleon.Modding.PlayfieldList>(Eleon.Modding.CmdId.Request_Playfield_List, null));
                }
                catch (Exception ex)
                {
                    _traceSource.TraceEvent(TraceEventType.Error, 1, "OnConnected Exception: {0}", ex.Message);
                    BreakIfDebugBuild();
                }
            };

            _client.Connect(_config.GameServerIp, _config.GameServerApiPort);
        }

        public void DebugOutput(String format, params object[] args)
        {
            _traceSource.TraceEvent(TraceEventType.Information, 2, format, args);
        }

        public void DebugLog(String format, params object[] args)
        {
            _traceSource.TraceEvent(TraceEventType.Verbose, 2, format, args);
        }

        public Task<T> SendRequest<T>(Eleon.Modding.CmdId cmdID, object data)
        {
            (var trackingId, var task) = _requestTracker.GetNewTaskCompletionSource<T>();

            SendRequest( cmdID, trackingId, data);

            return task;
        }

        public Task SendRequest(Eleon.Modding.CmdId cmdID, object data)
        {
            (var trackingId, var task) = _requestTracker.GetNewTaskCompletionSource<object>();

            SendRequest(cmdID, trackingId, data);

            return task;
        }

        public Task SendChatMessageToAll(string format, params object[] args)
        {
            string msg = format.SafeFormat(args);
            string command = $"SAY '{msg}'";
            DebugOutput("SendChatMessageToAll(\"{0}\")", msg);
            return SendRequest(
                Eleon.Modding.CmdId.Request_ConsoleCommand,
                new Eleon.Modding.PString(command));
        }

        public Task SendAlarmMessageToAll(string format, params object[] args)
        {
            return SendMessageToAll(MessagePriority.Alarm, 1000, format, args);
        }

        public Task SendAlertMessageToAll(string format, params object[] args)
        {
            return SendMessageToAll(MessagePriority.Alert, 1000, format, args);
        }

        public Task SendAttentionMessageToAll(string format, params object[] args)
        {
            return SendMessageToAll(MessagePriority.Attention, 1000, format, args);
        }

        public Task SendMessageToAll(MessagePriority priority, float time, string format, params object[] args)
        {
            string msg = format.SafeFormat(args);
            return SendRequest(
                Eleon.Modding.CmdId.Request_InGameMessage_AllPlayers,
                new Eleon.Modding.IdMsgPrio(0, msg, (byte)priority, time));
        }

        public Task RequestEntitySpawn(EntitySpawnInfo entitySpawnInfo)
        {
            return SendRequest(Eleon.Modding.CmdId.Request_Entity_Spawn, entitySpawnInfo);
        }

        public Task RequestPlayfieldLoad(PlayfieldLoad playfieldLoad)
        {
            // PlayfieldLoad (sec = empty playfield hold time, processId not used)
            return SendRequest(Eleon.Modding.CmdId.Request_Load_Playfield, playfieldLoad);
        }

        public Task RequestConsoleCommand(string commandString)
        {
            return SendRequest(Eleon.Modding.CmdId.Request_ConsoleCommand, new PString(commandString));
        }

        public void AddBoundingBox( BoundingBox boundingBox)
        {
            lock(_bboxes)
            {
                _bboxes.Add(boundingBox);
            }
        }

        public Dictionary<int, Player> GetOnlinePlayers()
        {
            return _onlinePlayersInfoById;
        }

        public Player GetOnlinePlayerByName(string playerName)
        {
            lock(_onlinePlayersInfoById)
            {
                foreach(Player player in _onlinePlayersInfoById.Values)
                {
                    if(player.Name.Equals(playerName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return player;
                    }
                }
            }

            return null; // not found
        }

        public Playfield GetPlayfield(string playfieldName)
        {
            lock(_playfieldsByName)
            {
                if (!_playfieldsByName.ContainsKey(playfieldName))
                {
                    _playfieldsByName[playfieldName] = new Playfield(this, playfieldName);
                }

                return _playfieldsByName[playfieldName];
            }
        }

        public Faction GetFaction(int factionId)
        {
            lock (_factionsById)
            {
                if (_factionsById.ContainsKey(factionId))
                {
                    return _factionsById[factionId];
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task RefreshFactionList()
        {
            ProcessEvent_Get_Factions(await SendRequest<Eleon.Modding.FactionInfoList>(Eleon.Modding.CmdId.Request_Get_Factions, new Eleon.Modding.Id(k_RequestFactionListStartingFromId)));
        }

        public Structure GetStructure(int structureId)
        {
            lock (_playfieldsByName)
            {
                foreach (var kv in _playfieldsByName)
                {
                    var structuresById = kv.Value.StructuresById;
                    lock (structuresById)
                    {
                        if (structuresById.ContainsKey(structureId))
                        {
                            return structuresById[structureId];
                        }
                    }
                }
            }

            return null;
        }

        #endregion

        #region Private Helper Methods

        private void SendRequest(Eleon.Modding.CmdId cmdID, ushort seqNr, object data)
        {
            _traceSource.TraceEvent(TraceEventType.Information, 1, "SendRequest: Command {0} SeqNr: {1}", cmdID, seqNr);
            _client.Send(cmdID, (ushort)seqNr, data);
        }

        private void OnClient_GameEventReceived(EPMConnector.ModProtocol.Package p)
        {
            try
            {
                DebugLog("OnClient_GameEventReceived: Command {0} SeqNr: {1}", p.cmd, p.seqNr);

                if(p.cmd == Eleon.Modding.CmdId.Event_Error)
                {
                    Eleon.Modding.ErrorInfo eInfo = (Eleon.Modding.ErrorInfo)p.data;
                    Eleon.Modding.CmdId cmdId = (Eleon.Modding.CmdId)p.seqNr;
                    DebugLog("Event_Error - ErrorType {0}, CmdId {1}", eInfo.errorType, cmdId);
                }

                if (!_requestTracker.TryHandleEvent(p))
                {
                    if (p.data == null)
                    {
                        DebugLog("Empty Package cmd:{0}, seqnr:{1}", p.cmd, p.seqNr);
                        //System.Diagnostics.Debugger.Break();
                        return;
                    }

                    switch (p.cmd)
                    {
                        case Eleon.Modding.CmdId.Event_Playfield_Loaded:
                            ProcessEvent_Playfield_Loaded((Eleon.Modding.PlayfieldLoad)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Playfield_Unloaded:
                            ProcessEvent_Playfield_Unloaded((Eleon.Modding.PlayfieldLoad)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Playfield_List:
                            //ProcessEvent_Playfield_List((Eleon.Modding.PlayfieldList)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Playfield_Stats:
                            //ProcessEvent_Playfield_Stats((Eleon.Modding.PlayfieldStats)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Dedi_Stats:
                            ProcessEvent_Dedi_Stats((Eleon.Modding.DediStats)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_GlobalStructure_List:
                            ProcessEvent_GlobalStructure_List((Eleon.Modding.GlobalStructureList)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Structure_BlockStatistics:
                            ProcessEvent_Structure_BlockStatistics((Eleon.Modding.IdStructureBlockInfo)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Player_Connected:
                            ProcessEvent_Player_Connected(entityId: ((Eleon.Modding.Id)p.data).id);
                            break;

                        case Eleon.Modding.CmdId.Event_Player_Disconnected:
                            ProcessEvent_Player_Disconnected(entityId: ((Eleon.Modding.Id)p.data).id);
                            break;

                        case Eleon.Modding.CmdId.Event_Player_ChangedPlayfield:
                            ProcessEvent_Player_ChangedPlayfield((Eleon.Modding.IdPlayfield)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Player_Info:
                            ProcessEvent_Player_Info((Eleon.Modding.PlayerInfo)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Player_List:
                            //ProcessEvent_Player_List((Eleon.Modding.IdList)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Player_Inventory:
                            ProcessEvent_Player_Inventory((Eleon.Modding.Inventory)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Player_Credits:
                            ProcessEvent_Player_Credits((Eleon.Modding.IdCredits)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Player_ItemExchange:
                            ProcessEvent_Player_ItemExchange((Eleon.Modding.ItemExchangeInfo)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Player_DisconnectedWaiting:
                            ProcessEvent_Player_DisconnectedWaiting((Eleon.Modding.Id)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Entity_PosAndRot:
                            ProcessEvent_Entity_PosAndRot((Eleon.Modding.IdPositionRotation)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Faction_Changed:
                            ProcessEvent_Faction_Changed((Eleon.Modding.FactionChangeInfo)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Get_Factions:
                            ProcessEvent_Get_Factions((Eleon.Modding.FactionInfoList)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Statistics:
                            ProcessEvent_Statistics((Eleon.Modding.StatisticsParam)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_NewEntityId:
                            // from Request_NewEntityId
                            DebugLog("Event_NewEntityId - New ID: {0}", ((Eleon.Modding.Id)p.data).id);
                            break;

                        case Eleon.Modding.CmdId.Event_AlliancesAll:
                            ProcessEvent_AlliancesAll((Eleon.Modding.AlliancesTable)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_AlliancesFaction:
                            ProcessEvent_AlliancesFaction((Eleon.Modding.AlliancesFaction)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_ChatMessage:
                            ProcessEvent_ChatMessage((Eleon.Modding.ChatInfo)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_BannedPlayers:
                            ProcessEvent_BannedPlayers((Eleon.Modding.BannedPlayerData)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Ok:
                            DebugLog("Event_Ok - seqnr {0}", p.seqNr);
                            break;

                        case Eleon.Modding.CmdId.Event_Error:
                            {
                                Eleon.Modding.ErrorInfo eInfo = (Eleon.Modding.ErrorInfo)p.data;
                                Eleon.Modding.CmdId cmdId = (Eleon.Modding.CmdId)p.seqNr;
                                DebugOutput("Event_Error - ErrorType {0}, CmdId {1}", eInfo.errorType, cmdId);
                                BreakIfDebugBuild();
                            }
                            break;

                        case Eleon.Modding.CmdId.Event_TraderNPCItemSold:
                            ProcessEvent_TraderNPCItemSold((Eleon.Modding.TraderNPCItemSoldInfo)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Player_GetAndRemoveInventory:
                            ProcessEvent_Player_GetAndRemoveInventory((Eleon.Modding.Inventory)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_Playfield_Entity_List:
                            ProcessEvent_Playfield_Entity_List((Eleon.Modding.PlayfieldEntityList)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_ConsoleCommand:
                            ProcessEvent_ConsoleCommand((Eleon.Modding.ConsoleCommandInfo)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_PdaStateChange:
                            ProcessEvent_PdaStateChange((Eleon.Modding.PdaStateInfo)p.data);
                            break;

                        case Eleon.Modding.CmdId.Event_GameEvent:
                            ProcessEvent_GameEvent((Eleon.Modding.GameEventData)p.data);
                            break;

                        default:
                            DebugOutput("(1) Unknown package cmd {0}", p.cmd);
                            BreakIfDebugBuild();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _traceSource.TraceEvent(TraceEventType.Error, 1, "OnClient_GameEventReceived Exception: {0}", ex.Message);
                BreakIfDebugBuild();
            }
        }

        private void ProcessEvent_TraderNPCItemSold(TraderNPCItemSoldInfo obj)
        {
            DebugLog("Event_TraderNPCItemSold - Trader NPC item sold info: TraderType: {0}, TraderId: {1}, PlayerId: {2}, StructureId: {3}, Item: {4}, Amount: {5}, Price: {6}", obj.traderType, obj.traderEntityId, obj.playerEntityId, obj.structEntityId, obj.boughtItemId, obj.boughtItemCount, obj.boughtItemPrice);
        }

        private void ProcessEvent_GameEvent(GameEventData obj)
        {
            DebugLog("Event_GameEvent - type: {0}, name: {1}, type: {2}, amount: {3}, playerid: {4}, flag: {5}", obj.EventType, obj.Name, obj.Type, obj.Amount, obj.PlayerId, obj.Flag);
        }

        private void ProcessEvent_PdaStateChange(PdaStateInfo obj)
        {
            DebugLog("Event_PdaStateChange - Name:{0}, StateChange:{1}, PlayerId:{2}", obj.Name, obj.StateChange, obj.PlayerId);
        }

        private void ProcessEvent_ConsoleCommand(ConsoleCommandInfo obj)
        {
            DebugOutput("Player {0}; Console command: {1} Allowed: {2}", obj.playerEntityId, obj.command, obj.allowed);
        }

        private void ProcessEvent_Playfield_Entity_List(PlayfieldEntityList obj)
        {
            // from Request_Playfield_Entity_List
            DebugLog("Event_Playfield_Entity_List - Entities. Count: {0}", obj.entities != null ? obj.entities.Count : 0);

            //if (obj.entities != null)
            //{
            //    DebugLog("Playfield {0}", obj.playfield);

            //    foreach (Eleon.Modding.EntityInfo g in obj.entities)
            //    {
            //        DebugLog("  id={0} type={1} playfield={2} pos={3}/{4}/{5}", g.id, g.type, obj.playfield, g.pos.x, g.pos.y, g.pos.z);
            //    }
            //}
        }

        private void ProcessEvent_BannedPlayers(BannedPlayerData obj)
        {
            // from Request_GetBannedPlayers

            DebugLog("Event_BannedPlayers - Banned list. Count: {0}", obj.BannedPlayers != null ? obj.BannedPlayers.Count : 0);
            //foreach (Eleon.Modding.BannedPlayerData.BanEntry ba in obj.BannedPlayers)
            //{
            //    DebugLog("Id: {0}, Date: {1}", ba.steam64Id, DateTime.FromBinary(ba.dateTime));
            //}
        }

        private void ProcessEvent_AlliancesAll(AlliancesTable obj)
        {
            // from Request_AlliancesAll

            DebugLog("Event_AlliancesAll");


            //int facId1;
            //int facId2;

            ////Only differences to default alliances are listed (everyone in same Origin is by default allied)
            //foreach (int factionHash in obj.alliances)
            //{
            //    facId1 = (factionHash >> 16) & 0xffff;
            //    facId2 = factionHash & 0xffff;

            //    DebugLog("Alliance difference between faction {0} and faction {1}", facId1, facId2);
            //}
        }

        private void ProcessEvent_Statistics(StatisticsParam obj)
        {
            DebugLog("Event_Statistics - {0} {1} {2} {3} {4}", obj.type, obj.int1, obj.int2, obj.int3, obj.int4);

            //CoreRemoved,    int1: Structure id, int2: destryoing entity id, int3: (optional) controlling entity id
            //CoreAdded,      int1: Structure id, int2: destryoing entity id, int3: (optional) controlling entity id
            //PlayerDied,     // int1: player entity id, int2: death type (Unknown = 0,Projectile = 1,Explosion = 2,Food = 3,Oxygen = 4,Disease = 5,Drowning = 6,Fall = 7,Suicide = 8), int3: (optional) other entity involved, int4: (optional) other entity CV/SV/HV id
            //StructOnOff,    int1: structure id, int2: changing entity id, int3: 0 = off, 1 = on
            //StructDestroyed,// int1: structure id, int2: type (0=wipe, 1=decay)

            if (obj.type == StatisticsType.PlayerDied)
            {
                HandleDeadPlayer(obj);
            }
        }

        private void ProcessEvent_Faction_Changed(FactionChangeInfo obj)
        {
            DebugLog("Event_Faction_Changed - Faction changed entity: {0} faction id: {1} faction {2}", obj.id, obj.factionId, obj.factionGroup);
            Event_Faction_Changed?.Invoke(obj);
        }

        private void ProcessEvent_AlliancesFaction(AlliancesFaction obj)
        {
            // from Request_AlliancesFaction ?
            DebugLog("Event_AlliancesFaction - faction1Id:{0}, faction2Id:{1}, isAllied:{2}", obj.faction1Id, obj.faction2Id, obj.isAllied);
        }

        private void ProcessEvent_Player_GetAndRemoveInventory(Inventory inv)
        {
            // from Request_Player_GetAndRemoveInventory
            DebugLog("Event_Player_GetAndRemoveInventory - Got and removed Inventory from player {0}", inv.playerId);
            //if (inv.toolbelt != null)
            //{
            //    DebugLog("Toolbelt:");
            //    for (int i = 0; inv.toolbelt != null && i < inv.toolbelt.Length; i++)
            //    {
            //        DebugLog("  " + inv.toolbelt[i].slotIdx + ". " + inv.toolbelt[i].id + " " + inv.toolbelt[i].count + " " + inv.toolbelt[i].ammo);
            //    }
            //}
            //if (inv.bag != null)
            //{
            //    DebugLog("Bag:");
            //    for (int i = 0; inv.bag != null && i < inv.bag.Length; i++)
            //    {
            //        DebugLog("  " + inv.bag[i].slotIdx + ". " + inv.bag[i].id + " " + inv.bag[i].count + " " + inv.bag[i].ammo);
            //    }
            //}
        }

        private void ProcessEvent_Entity_PosAndRot(IdPositionRotation idPos)
        {
            // from Request_Entity_PosAndRot ?

            DebugLog("Event_Entity_PosAndRot - Entity with id {0} position {1}, {2}, {3} rotation {4}, {5}, {6}", idPos.id, idPos.pos.x, idPos.pos.y, idPos.pos.z, idPos.rot.x, idPos.rot.y, idPos.rot.z);
        }

        private void ProcessEvent_Player_DisconnectedWaiting(Id obj)
        {
            DebugOutput("Event_Player_DisconnectedWaiting - Player: {0}", obj.id);
        }

        private void ProcessEvent_Dedi_Stats(DediStats obj)
        {
            // from Request_Dedi_Stats
            DebugOutput("Event_Dedi_Stats - fps:{0}, mem:{1}, players:{2}, uptime:{3}, ticks:{4}", obj.fps, obj.mem, obj.players, obj.uptime, obj.ticks);
        }

        private void ProcessEvent_Player_ItemExchange(ItemExchangeInfo itemExchangeInfo)
        {
            // from Request_Player_ItemExchange
            DebugLog("Event_Player_ItemExchange - Player: {0}", itemExchangeInfo.id);
        }

        private void ProcessEvent_Player_Credits(IdCredits idCredits)
        {
            DebugLog("Event_Player_Credits - Credits player with id {0}: {1}", idCredits.id, idCredits.credits);

            // TODO: update player credits here
        }

        private void ProcessEvent_Player_Inventory(Inventory inv)
        {
            // from Request_Player_GetInventory
            // and Request_Player_SetInventory ?
            DebugLog("Event_Player_Inventory - Inventory received from player {0}", inv.playerId);
            //if (inv.toolbelt != null)
            //{
            //    DebugLog("Toolbelt:", p.cmd);
            //    for (int i = 0; inv.toolbelt != null && i < inv.toolbelt.Length; i++)
            //    {
            //        DebugLog("  " + inv.toolbelt[i].slotIdx + ". " + inv.toolbelt[i].id + " " + inv.toolbelt[i].count + " " + inv.toolbelt[i].ammo, p.cmd);
            //    }
            //}
            //if (inv.bag != null)
            //{
            //    DebugLog("Bag:", p.cmd);
            //    for (int i = 0; inv.bag != null && i < inv.bag.Length; i++)
            //    {
            //        DebugLog("  " + inv.bag[i].slotIdx + ". " + inv.bag[i].id + " " + inv.bag[i].count + " " + inv.bag[i].ammo, p.cmd);
            //    }
            //}
        }

        private void ProcessEvent_Structure_BlockStatistics(IdStructureBlockInfo obj)
        {
            // from Request_Structure_BlockStatistics

            //if (obj.blockStatistics == null) { break; }

            DebugLog("Event_Structure_BlockStatistics - Block statistic for {0}", obj.id);

            //foreach (KeyValuePair<int, int> blockstat in obj.blockStatistics)
            //{
            //    DebugOutput("Item {0}: Amount: {1}", blockstat.Key, blockstat.Value);
            //}
        }

        private void ProcessEvent_Player_ChangedPlayfield(IdPlayfield obj)
        {
            DebugLog("Event_Player_ChangedPlayfield - Player with id {0} changes to playfield {1}", obj.id, obj.playfield);

            lock (_onlinePlayersInfoById)
            {
                if (_onlinePlayersInfoById.ContainsKey(obj.id))
                {
                    var player = _onlinePlayersInfoById[obj.id];

                    var newPlayfield = GetPlayfield(obj.playfield);

                    Event_Player_ChangedPlayfield?.Invoke(newPlayfield, player);

                    // update data
                    player.UpdateInfo(newPlayfield);
                }
            }
        }

        private void ProcessEvent_Player_Disconnected(int entityId)
        {
            DebugOutput("Event_Player_Disconnected - Player with id {0} disconnected", entityId);

            lock (_onlinePlayersInfoById)
            {
                if (_onlinePlayersInfoById.TryGetValue(entityId, out Player player))
                {
                    _onlinePlayersInfoById.Remove(entityId);

                    Event_Player_Disconnected?.Invoke(player);
                }
                
            }
        }

        private async void ProcessEvent_Player_Connected(int entityId)
        {
            DebugOutput("Event_Player_Connected- Player with id {0} connected", entityId);

            Player player = ProcessEvent_Player_Info(await SendRequest<Eleon.Modding.PlayerInfo>(Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(entityId)));

            Event_Player_Connected?.Invoke(player);
        }

        private void ProcessEvent_GlobalStructure_List(GlobalStructureList obj)
        {
            // from Request_GlobalStructure_List
            // and Request_GlobalStructure_Update ?
            DebugLog("Event_GlobalStructure_List - Global structures. Count: {0}", obj.globalStructures != null ? obj.globalStructures.Count : 0);

            //if (obj.globalStructures != null)
            //{
            //    foreach (KeyValuePair<string, List<Eleon.Modding.GlobalStructureInfo>> kvp in obj.globalStructures)
            //    {
            //        DebugLog("Playfield {0}", kvp.Key);

            //        foreach (Eleon.Modding.GlobalStructureInfo g in kvp.Value)
            //        {
            //            DebugLog("  id={0} name={1} type={2} #blocks={3} #devices={4} playfield={5} pos={6}/{7}/{8}", g.id, g.name, g.type, g.cntBlocks, g.cntDevices, kvp.Key, g.pos.x, g.pos.y, g.pos.z);
            //        }
            //    }
            //}
        }

        private void ProcessEvent_Playfield_Unloaded(PlayfieldLoad obj)
        {
            DebugLog("Event_Playfield_Unloaded - Playfield {0} unloaded pid={1}", obj.playfield, obj.processId);

            //lock (_playfieldsByName)
            //{
            //    // TODO: update playfields with info
            //}
        }

        private void ProcessEvent_Playfield_Loaded(PlayfieldLoad playfieldLoadData)
        {
            DebugLog("Event_Playfield_Loaded - Playfield {0} loaded pid={1}", playfieldLoadData.playfield, playfieldLoadData.processId);

            lock (_playfieldsByName)
            {
                var playfield = GetPlayfield(playfieldLoadData.playfield);
                playfield.UpdateInfo(playfieldLoadData);

                if (Event_Playfield_Loaded != null)
                {
                    SendRequest<Eleon.Modding.GlobalStructureList>(Eleon.Modding.CmdId.Request_GlobalStructure_Update, new Eleon.Modding.PString(playfieldLoadData.playfield))
                        .ContinueWith((task) =>
                        {
                            playfield.UpdateInfo(task.Result);

                        // call event only after structures have been updated.
                        Event_Playfield_Loaded?.Invoke(playfield);
                        });
                }
            }
        }

        void HandleDeadPlayer(Eleon.Modding.StatisticsParam obj)
        {
            //PlayerDied,
            // int1: player entity id
            // int2: death type (Unknown = 0,Projectile = 1,Explosion = 2,Food = 3,Oxygen = 4,Disease = 5,Drowning = 6,Fall = 7,Suicide = 8)
            // int3: (optional) other entity involved
            // int4: (optional) other entity CV/SV/HV id
            if (GetOnlinePlayers().ContainsKey(obj.int1))
            {
                Player deadPlayer = GetOnlinePlayers()[obj.int1];

                var playerDeathInfo = new PlayerDeathInfo();

                if (GetOnlinePlayers().ContainsKey(obj.int3))
                {
                    playerDeathInfo.killer = GetOnlinePlayers()[obj.int3];
                }

                Event_PlayerDied?.Invoke(deadPlayer, playerDeathInfo);
            }
        }

        private void ProcessEvent_Get_Factions(FactionInfoList obj)
        {
            // from Request_Get_Factions
            if (obj != null)
            {
                DebugLog("Event_Get_Factions- Faction list. Count: {0}", obj.factions != null ? obj.factions.Count : 0);
                if (obj.factions != null)
                {
                    lock (_factionsById)
                    {
                        // TODO: remove deleted factions
                        foreach (Eleon.Modding.FactionInfo factionInfo in obj.factions)
                        {
                            DebugLog("Id: {0}, Abrev: {1}, Name: {2}, Origin: {3}", factionInfo.factionId, factionInfo.abbrev, factionInfo.name, factionInfo.origin);

                            if (!_factionsById.ContainsKey(factionInfo.factionId))
                            {
                                _factionsById[factionInfo.factionId] = new Faction(this, factionInfo);
                            }
                            else
                            {
                                _factionsById[factionInfo.factionId].UpdateInfo(factionInfo);
                            }
                        }
                    }
                }
            }
            else
            {
                _traceSource.TraceEvent(TraceEventType.Warning, 1, "ProcessEvent_Get_Factions got null object");
            }
        }

        private void ProcessEvent_Playfield_Stats(Eleon.Modding.PlayfieldStats playfieldStats)
        {
            // from Request_Playfield_Stats
            DebugLog("Event_Playfield_Stats - {0}: fps={1} heap={2} procid={3}", playfieldStats.playfield, playfieldStats.fps, playfieldStats.mem, playfieldStats.processId);

            lock (_playfieldsByName)
            {
                var playfield = GetPlayfield(playfieldStats.playfield);
                playfield.UpdateInfo(playfieldStats);
                SendRequest<Eleon.Modding.PlayfieldEntityList>(Eleon.Modding.CmdId.Request_Playfield_Entity_List, new Eleon.Modding.PString(playfieldStats.playfield))
                    .ContinueWith((task) => playfield.UpdateInfo(task.Result));
                SendRequest<Eleon.Modding.GlobalStructureList>(Eleon.Modding.CmdId.Request_GlobalStructure_Update, new Eleon.Modding.PString(playfieldStats.playfield))
                    .ContinueWith((task) => playfield.UpdateInfo(task.Result));
            }
        }

        private void ProcessEvent_Playfield_List(PlayfieldList playfieldList)
        {
            // from Request_Playfield_List
            DebugLog("Event_Playfield_List - Playfield list count: {0}", (playfieldList != null) ? playfieldList.playfields.Count : 0);

            //if (playfieldList != null)
            //{
            //    lock (_playfieldsByName)
            //    {
            //        foreach (string s in playfieldList.playfields)
            //        {
            //            DebugLog("  {0}", s);
            //            SendRequest<Eleon.Modding.PlayfieldStats>(Eleon.Modding.CmdId.Request_Playfield_Stats, new Eleon.Modding.PString(s))
            //                 .ContinueWith((task) => this.ProcessEvent_Playfield_Stats(task.Result));
            //        }
            //    }
            //}
        }

        private void ProcessEvent_Player_List(Eleon.Modding.IdList idList)
        {
            // from Request_Player_List
            if ( idList != null )
            {
                var playerIds = idList.list;

                DebugOutput("Event_Player_List - Count: {0}", playerIds.Count);

                for (int i = 0; i < playerIds.Count; i++)
                {
                    DebugLog("{0} Player with id {1}", i + 1, playerIds[i]);
                    SendRequest<Eleon.Modding.PlayerInfo>(Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(playerIds[i]))
                        .ContinueWith((task) => this.ProcessEvent_Player_Info(task.Result));
                }
            }
            else
            {
                DebugLog("Event_Player_List - no players");
            }
        }

        private void ProcessEvent_ChatMessage(Eleon.Modding.ChatInfo obj)
        {
            var chatType = (ChatType)obj.type;
            var msg = obj.msg;

            if (chatType == ChatType.PlayerToServer)
            {
                if(msg.StartsWith("s! "))
                {
                    msg = msg.Substring(3);
                }
            }

            DebugLog("Event_ChatMessage: Player: {0}, Recepient: {1}, Recepient Faction: {2}, {3}, Message: '{4}'", obj.playerId, obj.recipientEntityId, obj.recipientFactionId, chatType, obj.msg);

            if (obj.type != 8 && obj.type != 7 && msg.ToUpper() == "!MODS")
            {
                foreach (var versionString in _versionStrings)
                {
                    SendChatMessageToAll(versionString);
                }
            }
            else
            {
                Player player;
                lock (_onlinePlayersInfoById)
                {
                    player = _onlinePlayersInfoById[obj.playerId];
                }

                Event_ChatMessage?.Invoke(chatType, msg, player);
            }
        }

        private Player ProcessEvent_Player_Info(Eleon.Modding.PlayerInfo pInfo)
        {
            // from Request_Player_Info
            DebugLog("Event_Player_Info - Player info: cid={1} eid={2} name={3} playfield={4} fac={5}", pInfo.clientId, pInfo.entityId, pInfo.playerName, pInfo.playfield, pInfo.factionId);

            Player player;

            lock (_onlinePlayersInfoById)
            {
                if (_onlinePlayersInfoById.TryGetValue(pInfo.entityId, out player))
                {
                    player.UpdateInfo(pInfo);
                }
                else
                {
                    player = new Player(this, pInfo);
                    _onlinePlayersInfoById[pInfo.entityId] = player;
                }

                lock (_bboxes)
                {
                    foreach (var bbox in _bboxes)
                    {
                        bbox.OnPlayerUpdate(player);
                    }
                }
            }

            return player;
        }

        private void BreakIfDebugBuild()
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                    _client.Disconnect();
                    _client = null;
                    _traceSource.Flush();
                    _traceSource.Close();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~GameServerConnection() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
