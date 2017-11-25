using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SharedCode
{
    public enum MessagePriority : byte
    {
        Alarm = 0,
        Alert = 1,
        Attention = 2,
    }

    public class GameServerConnection : IDisposable
    {
        #region Private Data

        List<string> _versionStrings = new List<string>();

        EPMConnector.Client _client = new EPMConnector.Client();

        Dictionary<int, Player> _onlinePlayersInfoById = new Dictionary<int, Player>();
        Timer _playerUpdateTimer;

        List<BoundingBox> _bboxes = new List<BoundingBox>();

        BaseConfiguration _config;

        class RequestTracker
        {
            private ushort _nextAvailableId = 12340;
            private Dictionary<ushort, (Type, object/*TaskCompletionSource<T>*/)> _taskCompletionSourcesById = new Dictionary<ushort, (Type, object)>();

            public (ushort, TaskCompletionSource<T>) GetNewTaskCompletionSource<T>()
            {
                if (_nextAvailableId == ushort.MaxValue)
                {
                    _nextAvailableId = 0;
                }

                var newId = _nextAvailableId++;

                var taskCompletionSource = new TaskCompletionSource<T>();
                _taskCompletionSourcesById[newId] = (typeof(T), taskCompletionSource);

                return (newId, taskCompletionSource);
            }

            // Remove it from the list
            public TaskCompletionSource<T> GetTaskCompletionSourceForId<T>(ushort trackingId)
            {
                System.Diagnostics.Debug.Assert(_taskCompletionSourcesById.ContainsKey(trackingId));

                (Type type, object tcs) = _taskCompletionSourcesById[trackingId];

                var taskCompletionSource = tcs as TaskCompletionSource<T>;
                _taskCompletionSourcesById.Remove(trackingId);

                return taskCompletionSource;
            }

            public bool TryFailTaskCompletionSourceForId(ushort trackingId, Exception ex)
            {
                bool trackingIdFound = _taskCompletionSourcesById.ContainsKey(trackingId);

                if (trackingIdFound)
                {
                    (Type type, object tcs) = _taskCompletionSourcesById[trackingId];
                    _taskCompletionSourcesById.Remove(trackingId);

                    dynamic taskCompletionSource = Convert.ChangeType(tcs, type);

                    taskCompletionSource.SetException(ex);
                }

                return trackingIdFound;
            }

        }

        private RequestTracker _requestTracker = new RequestTracker();

        #endregion

        #region Public Events

        public event Action<Playfield, Player> Event_Player_ChangedPlayfield;

        public event Action<Eleon.Modding.ChatInfo, Player> Event_ChatMessage;

        public event Action<Eleon.Modding.FactionChangeInfo> Event_Faction_Changed;

        #endregion

        #region Public Methods

        public GameServerConnection( BaseConfiguration config )
        {
            _config = config;
            _playerUpdateTimer = new Timer(config.PlayerUpdateIntervalInSeconds * 1000);
            _playerUpdateTimer.Elapsed += OnPlayerUpdateTimer_Elapsed;

            _client.GameEventReceived += Client_GameEventReceived;
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
            _client.OnConnected += () =>
            {
                // Request player list to start off
                SendRequest(Eleon.Modding.CmdId.Request_Player_List, Eleon.Modding.CmdId.Request_Player_List, null);
            };

            _client.Connect(_config.GameServerIp, _config.GameServerApiPort);
        }

        public void DebugOutput(String format, params object[] args)
        {
            Console.Out.WriteLine(string.Format("Output: {0}", string.Format(format, args)));
        }

        public void SendRequest(Eleon.Modding.CmdId cmdID, Eleon.Modding.CmdId seqNr, object data)
        {
            SendRequest(cmdID, (ushort)seqNr, data);
        }

        public void SendRequest(Eleon.Modding.CmdId cmdID, ushort seqNr, object data)
        {
            DebugOutput("SendRequest: Command {0} SeqNr: {1}", cmdID, seqNr);
            _client.Send(cmdID, (ushort)seqNr, data);
        }

        public void SendChatMessageToAll(string format, params object[] args)
        {
            string msg = string.Format(format, args);
            string command = "SAY '" + msg + "'";
            SendRequest(
                Eleon.Modding.CmdId.Request_ConsoleCommand,
                Eleon.Modding.CmdId.Request_InGameMessage_AllPlayers,
                new Eleon.Modding.PString(command));
            DebugOutput("ChatMessage(\"{0}\")", msg);
        }

        public async Task<Eleon.Modding.ItemExchangeInfo> DoItemExchangeWithPlayer(Player player, string title, string description, string buttonText, Eleon.Modding.ItemStack[] items = null )
        {
            (var trackingId, var taskCompletionSource) = _requestTracker.GetNewTaskCompletionSource<Eleon.Modding.ItemExchangeInfo>();

            var data = new Eleon.Modding.ItemExchangeInfo();
            data.id = player.EntityId;
            data.title = title;
            data.desc = description;
            data.buttonText = buttonText;
            data.items = items;

            SendRequest(
                Eleon.Modding.CmdId.Request_Player_ItemExchange,
                trackingId,
                data);

            return await taskCompletionSource.Task;
        }

        public void SendAlarmlMessageToAll(string format, params object[] args)
        {
            SendMessageToAll(MessagePriority.Alarm, 100, format, args);
        }

        public void SendAlertMessageToAll(string format, params object[] args)
        {
            SendMessageToAll(MessagePriority.Alert, 100, format, args);
        }

        public void SendAttentionMessageToAll(string format, params object[] args)
        {
            SendMessageToAll(MessagePriority.Attention, 100, format, args);
        }

        public void SendMessageToAll(MessagePriority priority, float time, string format, params object[] args)
        {
            string msg = string.Format(format, args);
            SendRequest(
                Eleon.Modding.CmdId.Request_InGameMessage_AllPlayers,
                Eleon.Modding.CmdId.Request_InGameMessage_AllPlayers,
                new Eleon.Modding.IdMsgPrio(0, msg, (byte)priority, time));
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

        #endregion

        #region Private Helper Methods

        private void Client_GameEventReceived(EPMConnector.ModProtocol.Package p)
        {
            try
            {
                if (p.data == null)
                {
                    DebugOutput("Empty Package cmd:{0}, seqnr:{1}", p.cmd, p.seqNr);
                    //System.Diagnostics.Debugger.Break();
                    return;
                }

                //Request_Structure_Touch = 11,
                //Request_Player_AddItem = 24,
                //Request_Player_Credits = 25,
                //Request_Player_SetCredits = 26,
                //Request_Player_AddCredits = 27,
                //Request_Blueprint_Finish = 29,
                //Request_Blueprint_Resources = 30,
                //Request_Player_ChangePlayerfield = 31,
                //Request_Player_SetPlayerInfo = 34,
                //Request_Entity_Teleport = 36,
                //Request_Entity_ChangePlayfield = 37,
                //Request_Entity_Destroy = 38,
                //Request_Entity_Spawn = 42,
                //Request_Load_Playfield = 52,
                //Request_ConsoleCommand = 54,
                //Request_InGameMessage_SinglePlayer = 57,
                //Request_InGameMessage_AllPlayers = 58,
                //Request_InGameMessage_Faction = 59,
                //Request_ShowDialog_SinglePlayer = 60,
                //Request_Entity_Destroy2 = 68,
                //Request_Entity_Export = 69,
                //Request_Entity_SetName = 71,


                switch (p.cmd)
                {
                    case Eleon.Modding.CmdId.Event_Playfield_Loaded:
                        {
                            Eleon.Modding.PlayfieldLoad obj = (Eleon.Modding.PlayfieldLoad)p.data;

                            DebugOutput("Event_Playfield_Loaded - Playfield {0} loaded pid={1}", obj.playfield, obj.processId);

                            // TODO: update playfields with info
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Playfield_Unloaded:
                        {
                            Eleon.Modding.PlayfieldLoad obj = (Eleon.Modding.PlayfieldLoad)p.data;

                            DebugOutput("Event_Playfield_Unloaded - Playfield {0} unloaded pid={1}", obj.playfield, obj.processId);

                            // TODO: update playfields with info
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Playfield_List:
                        {
                            // from Request_Playfield_List
                            Eleon.Modding.PlayfieldList obj = (Eleon.Modding.PlayfieldList)p.data;
                            //if (obj.playfields == null) { break; }
                            DebugOutput("Event_Playfield_List - Playfield list count: {0}", obj.playfields != null ? obj.playfields.Count : 0);

                            //System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => mainWindowDataContext.onlinePlayfields.Clear()));

                            //lock (playfields)
                            //{
                            //    playfields.Clear();
                            foreach (string s in obj.playfields)
                            {
                                //System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => mainWindowDataContext.onlinePlayfields.Add(s)));
                                DebugOutput("  {0}", s);
                                //playfields.Add(s);
                            }
                            //}
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Playfield_Stats:
                        {
                            // from Request_Playfield_Stats

                            Eleon.Modding.PlayfieldStats obj = (Eleon.Modding.PlayfieldStats)p.data;
                            DebugOutput("Event_Playfield_Stats - {0}: fps={1} heap={2} procid={3}", obj.playfield, obj.fps, obj.mem, obj.processId);

                            // TODO: update playfields with info
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Dedi_Stats:
                        {
                            // from Request_Dedi_Stats

                            Eleon.Modding.DediStats obj = (Eleon.Modding.DediStats)p.data;
                            DebugOutput("Event_Dedi_Stats - {0}fps", obj.fps);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_GlobalStructure_List:
                        {
                            // from Request_GlobalStructure_List
                            // and Request_GlobalStructure_Update ?

                            Eleon.Modding.GlobalStructureList obj = (Eleon.Modding.GlobalStructureList)p.data;
                            //if (obj.globalStructures == null) { break; }
                            DebugOutput("Event_GlobalStructure_List - Global structures. Count: {0}", obj.globalStructures != null ? obj.globalStructures.Count : 0);

                            if (obj.globalStructures != null)
                            {
                                //System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => mainWindowDataContext.structures.Clear()));

                                foreach (KeyValuePair<string, List<Eleon.Modding.GlobalStructureInfo>> kvp in obj.globalStructures)
                                {
                                    DebugOutput("Playfield {0}", kvp.Key);

                                    foreach (Eleon.Modding.GlobalStructureInfo g in kvp.Value)
                                    {
                                        //StructureInfo stI = new StructureInfo();
                                        //stI.FromStructureInfo(g, kvp.Key);

                                        DebugOutput("  id={0} name={1} type={2} #blocks={3} #devices={4} playfield={5} pos={6}/{7}/{8}", g.id, g.name, g.type, g.cntBlocks, g.cntDevices, kvp.Key, g.pos.x, g.pos.y, g.pos.z);

                                        //System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => mainWindowDataContext.structures.Add(stI)));
                                    }
                                }
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Structure_BlockStatistics:
                        {
                            // from Request_Structure_BlockStatistics

                            Eleon.Modding.IdStructureBlockInfo obj = (Eleon.Modding.IdStructureBlockInfo)p.data;
                            //if (obj.blockStatistics == null) { break; }

                            DebugOutput("Event_Structure_BlockStatistics - Block statistic for {0}", obj.id);

                            foreach (KeyValuePair<int, int> blockstat in obj.blockStatistics)
                            {
                                DebugOutput("Item {0}: Amount: {1}", blockstat.Key, blockstat.Value);
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_Connected:
                        {
                            int entityId = ((Eleon.Modding.Id)p.data).id;

                            DebugOutput("Event_Player_Connected- Player with id {0} connected", entityId);

                            SendRequest(Eleon.Modding.CmdId.Request_Player_Info, Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(entityId));
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_Disconnected:
                        {
                            int entityId = ((Eleon.Modding.Id)p.data).id;

                            DebugOutput("Event_Player_Disconnected - Player with id {0} disconnected", entityId);

                            lock (_onlinePlayersInfoById)
                            {
                                _onlinePlayersInfoById.Remove(entityId);
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_ChangedPlayfield:
                        {
                            Eleon.Modding.IdPlayfield obj = (Eleon.Modding.IdPlayfield)p.data;

                            DebugOutput("Event_Player_ChangedPlayfield - Player with id {0} changes to playfield {1}", obj.id, obj.playfield);

                            lock (_onlinePlayersInfoById)
                            {
                                if (_onlinePlayersInfoById.ContainsKey(obj.id))
                                {
                                    var player = _onlinePlayersInfoById[obj.id];

                                    // TODO: look up playfield object by name
                                    var newPlayfield = new Playfield(obj.playfield);

                                    Event_Player_ChangedPlayfield?.Invoke(newPlayfield, player);

                                    // update data
                                    player.UpdateInfo(newPlayfield);
                                }
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_Info:
                        {
                            // from Request_Player_Info
                            Eleon.Modding.PlayerInfo pInfo = (Eleon.Modding.PlayerInfo)p.data;
                            DebugOutput("Event_Player_Info - Player info (seqnr {0}): cid={1} eid={2} name={3} playfield={4} fac={5}", p.seqNr, pInfo.clientId, pInfo.entityId, pInfo.playerName, pInfo.playfield, pInfo.factionId);

                            Process_Event_Player_Info(p, pInfo);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_List:
                        {
                            // from Request_Player_List
                            DebugOutput("Event_Player_List");
                            var playerIds = ((Eleon.Modding.IdList)p.data).list;

                            for (int i = 0; i < playerIds.Count; i++)
                            {
                                SendRequest(Eleon.Modding.CmdId.Request_Player_Info, Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(playerIds[i]));
                                DebugOutput("{0} Player with id {1}", i + 1, playerIds[i]);
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_Inventory:
                        {
                            // from Request_Player_GetInventory
                            // and Request_Player_SetInventory ?

                            Eleon.Modding.Inventory inv = (Eleon.Modding.Inventory)p.data;

                            DebugOutput("Event_Player_Inventory - Inventory received from player {0}", inv.playerId);
                            if (inv.toolbelt != null)
                            {
                                DebugOutput("Toolbelt:", p.cmd);
                                for (int i = 0; inv.toolbelt != null && i < inv.toolbelt.Length; i++)
                                {
                                    DebugOutput("  " + inv.toolbelt[i].slotIdx + ". " + inv.toolbelt[i].id + " " + inv.toolbelt[i].count + " " + inv.toolbelt[i].ammo, p.cmd);
                                }
                            }
                            if (inv.bag != null)
                            {
                                DebugOutput("Bag:", p.cmd);
                                for (int i = 0; inv.bag != null && i < inv.bag.Length; i++)
                                {
                                    DebugOutput("  " + inv.bag[i].slotIdx + ". " + inv.bag[i].id + " " + inv.bag[i].count + " " + inv.bag[i].ammo, p.cmd);
                                }
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_Credits:
                        {
                            Eleon.Modding.IdCredits idCredits = (Eleon.Modding.IdCredits)p.data;
                            DebugOutput("Event_Player_Credits - Credits player with id {0}: {1}", idCredits.id, idCredits.credits);

                            // TODO: update player credits here
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_ItemExchange:
                        {
                            // from Request_Player_ItemExchange

                            var itemExchangeInfo = (Eleon.Modding.ItemExchangeInfo)p.data;
                            DebugOutput("Event_Player_ItemExchange - Request: {0}, Player: {1}", p.seqNr, itemExchangeInfo.id);

                            var tcs = _requestTracker.GetTaskCompletionSourceForId<Eleon.Modding.ItemExchangeInfo>(p.seqNr);

                            tcs.SetResult(itemExchangeInfo);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_DisconnectedWaiting:
                        {
                            Eleon.Modding.Id obj = (Eleon.Modding.Id)p.data;
                            DebugOutput("Event_Player_DisconnectedWaiting - Player: {0}", obj.id);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Entity_PosAndRot:
                        {
                            // from Request_Entity_PosAndRot ?

                            Eleon.Modding.IdPositionRotation idPos = (Eleon.Modding.IdPositionRotation)p.data;
                            DebugOutput("Event_Entity_PosAndRot - Entity with id {0} position {1}, {2}, {3} rotation {4}, {5}, {6}", idPos.id, idPos.pos.x, idPos.pos.y, idPos.pos.z, idPos.rot.x, idPos.rot.y, idPos.rot.z);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Faction_Changed:
                        {
                            Eleon.Modding.FactionChangeInfo obj = (Eleon.Modding.FactionChangeInfo)p.data;
                            DebugOutput("Event_Faction_Changed - Faction changed entity: {0} faction id: {1} faction {2}", obj.id, obj.factionId, obj.factionGroup);

                            Event_Faction_Changed?.Invoke(obj);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Get_Factions:
                        {
                            // from Request_Get_Factions

                            Eleon.Modding.FactionInfoList obj = (Eleon.Modding.FactionInfoList)p.data;
                            //if (obj.factions == null) { break; }
                            DebugOutput("Event_Get_Factions- Faction list. Count: {0}", obj.factions != null ? obj.factions.Count : 0);
                            foreach (Eleon.Modding.FactionInfo fI in obj.factions)
                            {
                                DebugOutput("Id: {0}, Abrev: {1}, Name: {2}, Origin: {3}", fI.factionId, fI.abbrev, fI.name, fI.origin);
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Statistics:
                        {
                            Eleon.Modding.StatisticsParam obj = (Eleon.Modding.StatisticsParam)p.data;

                            DebugOutput("Event_Statistics - {0} {1} {2} {3} {4}", obj.type, obj.int1, obj.int2, obj.int3, obj.int4);

                            //CoreRemoved,    int1: Structure id, int2: destryoing entity id, int3: (optional) controlling entity id
                            //CoreAdded,      int1: Structure id, int2: destryoing entity id, int3: (optional) controlling entity id
                            //PlayerDied,     // int1: player entity id, int2: death type (Unknown = 0,Projectile = 1,Explosion = 2,Food = 3,Oxygen = 4,Disease = 5,Drowning = 6,Fall = 7,Suicide = 8), int3: (optional) other entity involved, int4: (optional) other entity CV/SV/HV id
                            //StructOnOff,    int1: structure id, int2: changing entity id, int3: 0 = off, 1 = on
                            //StructDestroyed,// int1: structure id, int2: type (0=wipe, 1=decay)
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_NewEntityId:
                        {
                            // from Request_NewEntityId

                            DebugOutput("Event_NewEntityId - New ID: {0}", ((Eleon.Modding.Id)p.data).id);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_AlliancesAll:
                        {
                            // from Request_AlliancesAll

                            DebugOutput("Event_AlliancesAll");

                            Eleon.Modding.AlliancesTable obj = (Eleon.Modding.AlliancesTable)p.data;

                            int facId1;
                            int facId2;

                            //Only differences to default alliances are listed (everyone in same Origin is by default allied)
                            foreach (int factionHash in obj.alliances)
                            {
                                facId1 = (factionHash >> 16) & 0xffff;
                                facId2 = factionHash & 0xffff;

                                DebugOutput("Alliance difference between faction {0} and faction {1}", facId1, facId2);
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_AlliancesFaction:
                        {
                            // from Request_AlliancesFaction ?
                            var obj = (Eleon.Modding.AlliancesFaction)p.data;

                            DebugOutput("Event_AlliancesFaction");
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_ChatMessage:
                        {
                            Eleon.Modding.ChatInfo obj = (Eleon.Modding.ChatInfo)p.data;

                            string typeName;
                            switch (obj.type)
                            {
                                case 5: //?
                                case 7:
                                    typeName = "to faction";
                                    break;
                                case 8:
                                    typeName = "to player";
                                    break;
                                case 9:
                                    typeName = "to server";
                                    break;
                                default:
                                    typeName = "";
                                    break;
                            }

                            DebugOutput("Event_ChatMessage: Player: {0}, Recepient: {1}, Recepient Faction: {2}, {3}, Message: '{4}'", obj.playerId, obj.recipientEntityId, obj.recipientFactionId, typeName, obj.msg);

                            if (obj.type != 8 && obj.type != 7 && obj.msg == "!MODS")
                            {
                                foreach( var versionString in _versionStrings)
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

                                Event_ChatMessage?.Invoke(obj, player);
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_BannedPlayers:
                        {
                            // from Request_GetBannedPlayers

                            Eleon.Modding.BannedPlayerData obj = (Eleon.Modding.BannedPlayerData)p.data;
                            //if (obj.BannedPlayers == null) { break; }
                            DebugOutput("Event_BannedPlayers - Banned list. Count: {0}", obj.BannedPlayers != null ? obj.BannedPlayers.Count : 0);
                            foreach (Eleon.Modding.BannedPlayerData.BanEntry ba in obj.BannedPlayers)
                            {
                                DebugOutput("Id: {0}, Date: {1}", ba.steam64Id, DateTime.FromBinary(ba.dateTime));
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Ok:
                        {
                            DebugOutput("Event_Ok - seqnr {0}", p.seqNr);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Error:
                        {
                            Eleon.Modding.ErrorInfo eInfo = (Eleon.Modding.ErrorInfo)p.data;

                            if (!_requestTracker.TryFailTaskCompletionSourceForId(p.seqNr, new Exception(eInfo.ToString())))
                            {
                                Eleon.Modding.CmdId cmdId = (Eleon.Modding.CmdId)p.seqNr;
                                DebugOutput("Event_Error - ErrorType {0}, CmdId {1}", eInfo.errorType, cmdId);
                                System.Diagnostics.Debugger.Break();
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_TraderNPCItemSold:
                        {
                            Eleon.Modding.TraderNPCItemSoldInfo obj = (Eleon.Modding.TraderNPCItemSoldInfo)p.data;
                            DebugOutput("Event_TraderNPCItemSold - Trader NPC item sold info: TraderType: {0}, TraderId: {1}, PlayerId: {2}, StructureId: {3}, Item: {4}, Amount: {5}, Price: {6}", obj.traderType, obj.traderEntityId, obj.playerEntityId, obj.structEntityId, obj.boughtItemId, obj.boughtItemCount, obj.boughtItemPrice);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_GetAndRemoveInventory:
                        {
                            // from Request_Player_GetAndRemoveInventory

                            Eleon.Modding.Inventory inv = (Eleon.Modding.Inventory)p.data;
                            DebugOutput("Event_Player_GetAndRemoveInventory - Got and removed Inventory from player {0}", inv.playerId);
                            if (inv.toolbelt != null)
                            {
                                DebugOutput("Toolbelt:");
                                for (int i = 0; inv.toolbelt != null && i < inv.toolbelt.Length; i++)
                                {
                                    DebugOutput("  " + inv.toolbelt[i].slotIdx + ". " + inv.toolbelt[i].id + " " + inv.toolbelt[i].count + " " + inv.toolbelt[i].ammo);
                                }
                            }
                            if (inv.bag != null)
                            {
                                DebugOutput("Bag:");
                                for (int i = 0; inv.bag != null && i < inv.bag.Length; i++)
                                {
                                    DebugOutput("  " + inv.bag[i].slotIdx + ". " + inv.bag[i].id + " " + inv.bag[i].count + " " + inv.bag[i].ammo);
                                }
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Playfield_Entity_List:
                        {
                            // from Request_Playfield_Entity_List

                            Eleon.Modding.PlayfieldEntityList obj = (Eleon.Modding.PlayfieldEntityList)p.data;
                            if (obj.entities == null) { break; }
                            DebugOutput("Event_Playfield_Entity_List - Entities. Count: {0}", obj.entities != null ? obj.entities.Count : 0);

                            if (obj.entities != null)
                            {
                                DebugOutput("Playfield {0}", obj.playfield);

                                foreach (Eleon.Modding.EntityInfo g in obj.entities)
                                {
                                    DebugOutput("  id={0} type={1} playfield={2} pos={3}/{4}/{5}", g.id, g.type, obj.playfield, g.pos.x, g.pos.y, g.pos.z);
                                }
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_ConsoleCommand:
                        {
                            Eleon.Modding.ConsoleCommandInfo obj = (Eleon.Modding.ConsoleCommandInfo)p.data;
                            DebugOutput("Player {0}; Console command: {1} Allowed: {2}", obj.playerEntityId, obj.command, obj.allowed);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_PdaStateChange:
                        {
                            // from Request_AlliancesFaction ?
                            var obj = (Eleon.Modding.PdaStateInfo)p.data;

                            DebugOutput("Event_PdaStateChange");
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_GameEvent:
                        {
                            // from Request_AlliancesFaction ?
                            var obj = (Eleon.Modding.GameEventData)p.data;

                            DebugOutput("Event_GameEvent - type: {0}, name: {1}, type: {2}, amount: {3}, playerid: {4}, flag: {5}", obj.EventType, obj.Name, obj.Type, obj.Amount, obj.PlayerId, obj.Flag);
                        }
                        break;

                    default:
                        DebugOutput("(1) Unknown package cmd {0}", p.cmd);
                        System.Diagnostics.Debugger.Break();
                        break;
                }
            }
            catch (Exception ex)
            {
                DebugOutput("Exception: {0}", ex.Message);
                System.Diagnostics.Debugger.Break();
            }
        }

        private void Process_Event_Player_Info(EPMConnector.ModProtocol.Package p, Eleon.Modding.PlayerInfo pInfo)
        {
            lock (_onlinePlayersInfoById)
            {
                if (_onlinePlayersInfoById.ContainsKey(pInfo.entityId))
                {
                    _onlinePlayersInfoById[pInfo.entityId].UpdateInfo(pInfo);
                }
                else
                {
                    _onlinePlayersInfoById[pInfo.entityId] = new Player(this, pInfo);
                }

                lock (_bboxes)
                {
                    foreach (var bbox in _bboxes)
                    {
                        bbox.OnPlayerUpdate(_onlinePlayersInfoById[pInfo.entityId]);
                    }
                }
            }



            if (!_playerUpdateTimer.Enabled)
            {
                _playerUpdateTimer.Start();
            }
        }

        private void OnPlayerUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_onlinePlayersInfoById)
            {
                foreach (var entityId in _onlinePlayersInfoById.Keys)
                {
                    SendRequest(Eleon.Modding.CmdId.Request_Player_Info, Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(entityId));
                }
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
