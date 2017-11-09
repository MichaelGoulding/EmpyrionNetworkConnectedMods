using Eleon.Modding;
using EPMConnector;
using System;
using System.Collections.Generic;
using System.Timers;

namespace PlanetOwnership
{
    // TODO:  keep track of playfield ownership
    // TODO: create API to send messages to players, factions, etc.
    // TODO: have module that keeps track of loaded playfields and then queries for structures on that playfield if owned.
    // ask for structures for that playfield regularly, and if factionId != 0 && != owningFactionId, then prompt before destorying them in X minutes.


    class Program
    {
        static readonly string k_versionString = "PlanetOwnership 0.1 by Mortlath.";

        static EPMConnector.Client _client = new EPMConnector.Client();

        static BoundingBox _bbox = new BoundingBox();

        static Dictionary<int, Player> _players = new Dictionary<int, Player>();

        static Timer _playerUpdateTimer = new Timer(10 * 1000); // TODO: read config for time

        static Timer _factionRewardTimer = new Timer(10 * 1000); // TODO: read config for time

        static Config.Configuration config;

        static Config.SaveState saveState;

        static void Main(string[] args)
        {
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "Settings.yaml";
            var saveStateFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "SaveState.yaml";

            config = Config.Configuration.GetConfiguration(configFilePath);

            _factionRewardTimer = new Timer(config.CaptureRewardMinutes * 1000.0 * 60.0 );

            saveState = Config.SaveState.Load(saveStateFilePath);

            // connect to server
            _client.GameEventReceived += Client_GameEventReceived;
            _client.ClientMessages += Client_ClientMessages;

            OwnershipChangeChecker ownershipChangeChecker = new OwnershipChangeChecker(new Faction(0), _bbox);

            // get/refresh data
            _playerUpdateTimer.Elapsed += OnPlayerUpdateTimer_Elapsed;
            _factionRewardTimer.Elapsed += OnFactionRewardTimer_Elapsed;

            _client.Connect(config.GameServerIp, config.GameServerApiPort);

            SendRequest(Eleon.Modding.CmdId.Request_Player_List, Eleon.Modding.CmdId.Request_Player_List, null);

            // while forever
            string input = Console.ReadLine();

            _factionRewardTimer.Stop();
            _playerUpdateTimer.Stop();

            _client.Disconnect();

            _client = null;

            saveState.Save(saveStateFilePath);
        }

        private static void OnFactionRewardTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_players)
            {
                foreach (var playerId in _players.Keys)
                {
                    if (saveState.FactionIdToEntityIds.ContainsKey(_players[playerId].MemberOfFaction.Id) )// _players[entityId].MemberOfFaction.Id == saveState.OwningFactionId)
                    {
                        foreach(var entityId in saveState.FactionIdToEntityIds[_players[playerId].MemberOfFaction.Id])
                        {

                        }
                        var factoryContents = _players[playerId].BpResourcesInFactory;

                        List<ItemStack> itemStacks = new List<ItemStack>();
                        //var oldCobaltAmount = factoryContents.ContainsKey(ItemId.Ingot_Cobalt) ? (int)factoryContents[ItemId.Ingot_Cobalt] : 0;
                        //var oldSiliconAmount = factoryContents.ContainsKey(ItemId.Ingot_Silicon) ? (int)factoryContents[ItemId.Ingot_Silicon] : 0;
                        //itemStacks.Add(new ItemStack(2273, oldCobaltAmount + 2000));
                        //itemStacks.Add(new ItemStack(2274, oldSiliconAmount + 2000));
                        itemStacks.Add(new ItemStack(2273, 2000));
                        itemStacks.Add(new ItemStack(2274, 2000));

                        BlueprintResources bps = new BlueprintResources(playerId, itemStacks, false);

                        SendRequest(Eleon.Modding.CmdId.Request_Blueprint_Resources, Eleon.Modding.CmdId.Request_Blueprint_Resources, bps);
                    }
                }
            }
        }

        private static void OnPlayerUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_players)
            {
                foreach (var entityId in _players.Keys)
                {
                    SendRequest(Eleon.Modding.CmdId.Request_Player_Info, Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(entityId));
                }
            }

            SendRequest(Eleon.Modding.CmdId.Request_GlobalStructure_List, Eleon.Modding.CmdId.Request_GlobalStructure_List, null);
        }

        private static void Client_GameEventReceived(ModProtocol.Package p)
        {
            try
            {
                if (p.data == null)
                {
                    Output("Empty Package id rec: {0}", p.cmd);
                    return;
                }

                //if (mainWindowDataContext != null && mainWindowDataContext.EnableOutput_DataRecieved)
                {
                    //Output("Package received, id: {0}, type: {1}", p.cmd, Enum.GetName(cmdType, p.cmd));
                    //Output("received  event: c=" + p.cmd + " sNr=" + p.seqNr + " d=" + p.data + " client=" + client);
                }

                switch (p.cmd)
                {
                    case Eleon.Modding.CmdId.Event_Player_Connected:
                        {
                            int entityId = ((Eleon.Modding.Id)p.data).id;

                            Output("Player with id {0} connected", entityId);

                            SendRequest(Eleon.Modding.CmdId.Request_Player_Info, Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(entityId));

                            //lock (playerIds)
                            //{
                            //    playerIds.Add(entityId);
                            //}
                            //System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => mainWindowDataContext.onlinePlayer.Add(entityId)));
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_Disconnected:
                        {
                            int entityId = ((Eleon.Modding.Id)p.data).id;

                            Output("Player with id {0} disconnected", entityId);

                            lock (_players)
                            {
                                _players.Remove(entityId);
                            }
                            //System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => mainWindowDataContext.onlinePlayer.Remove(entityId)));
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_List:
                        {
                            if (p.data != null)
                            {  // empyt list is null?!
                                lock (_players)
                                {
                                    var playerIds = ((Eleon.Modding.IdList)p.data).list;

                                    //System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => mainWindowDataContext.onlinePlayer.Clear()));
                                    for (int i = 0; i < playerIds.Count; i++)
                                    {
                                        SendRequest(Eleon.Modding.CmdId.Request_Player_Info, Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(playerIds[i]));
                                        //System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => mainWindowDataContext.onlinePlayer.Add(playerIds[i])));
                                        Output("{0} Player with id {1}", i + 1, playerIds[i]);
                                    }
                                }
                            }
                            else
                            {
                                Output("No players connected");
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_Info:
                        {
                            Eleon.Modding.PlayerInfo pInfo = (Eleon.Modding.PlayerInfo)p.data;
                            if (pInfo == null) { break; }
                            Output("Player info (seqnr {0}): cid={1} eid={2} name={3} playfield={4} fac={5}", p.seqNr, pInfo.clientId, pInfo.entityId, pInfo.playerName, pInfo.playfield, pInfo.factionId);

                            lock (_players)
                            {
                                if (_players.ContainsKey(pInfo.entityId))
                                {
                                    _players[pInfo.entityId].UpdateInfo(pInfo);
                                }
                                else
                                {
                                    _players[pInfo.entityId] = new Player(pInfo);
                                }

                                _bbox.OnPlayerUpdate(_players[pInfo.entityId]);

                                if (!_playerUpdateTimer.Enabled)
                                {
                                    _playerUpdateTimer.Start();
                                }

                                if (!_factionRewardTimer.Enabled)
                                {
                                    _factionRewardTimer.Start();
                                }
                            }

                            //System.Windows.Application.Current.Dispatcher.Invoke((Action)(() =>
                            //{
                            //    mainWindowDataContext.playerInfos.Clear();
                            //    PlayerInfo plI = new PlayerInfo();
                            //    plI.FromPlayerInfo(pInfo);
                            //    mainWindowDataContext.playerInfos.Add(plI);
                            //}));
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_Inventory:
                        {
                            Eleon.Modding.Inventory inv = (Eleon.Modding.Inventory)p.data;
                            //if (inv == null) { break; }
                            //Output("Inventory received from player {0}", inv.playerId);
                            //if (inv.toolbelt != null)
                            //{
                            //    output("Toolbelt:", p.cmd);
                            //    for (int i = 0; inv.toolbelt != null && i < inv.toolbelt.Length; i++)
                            //    {
                            //        output("  " + inv.toolbelt[i].slotIdx + ". " + inv.toolbelt[i].id + " " + inv.toolbelt[i].count + " " + inv.toolbelt[i].ammo, p.cmd);
                            //    }
                            //}
                            //if (inv.bag != null)
                            //{
                            //    output("Bag:", p.cmd);
                            //    for (int i = 0; inv.bag != null && i < inv.bag.Length; i++)
                            //    {
                            //        output("  " + inv.bag[i].slotIdx + ". " + inv.bag[i].id + " " + inv.bag[i].count + " " + inv.bag[i].ammo, p.cmd);
                            //    }
                            //}
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Entity_PosAndRot:
                        {
                            Eleon.Modding.IdPositionRotation idPos = (Eleon.Modding.IdPositionRotation)p.data;
                            if (idPos == null) { break; }
                            Output("Player with id {0} position {1}, {2}, {3} rotation {4}, {5}, {6}", idPos.id, idPos.pos.x, idPos.pos.y, idPos.pos.z, idPos.rot.x, idPos.rot.y, idPos.rot.z);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_Credits:
                        {
                            Eleon.Modding.IdCredits idCredits = (Eleon.Modding.IdCredits)p.data;
                            if (idCredits == null) { break; }
                            Output("Credits player with id {0}: {1}", idCredits.id, idCredits.credits);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Ok:
                        {
                            Output("Event Ok seqnr {0}", p.seqNr);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Error:
                        {
                            Eleon.Modding.CmdId cmdId = (Eleon.Modding.CmdId)p.seqNr;
                            Eleon.Modding.ErrorInfo eInfo = (Eleon.Modding.ErrorInfo)p.data;

                            if (eInfo == null)
                            {
                                Output("Event Error seqnr {0}: TMD: p.data of Event_Error was not set", p.seqNr);
                            }
                            else
                            {
                                Output("Event Error {0} seqnr {1}", eInfo.errorType, cmdId);
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_ChangedPlayfield:
                        {
                            Eleon.Modding.IdPlayfield obj = (Eleon.Modding.IdPlayfield)p.data;

                            Output("Player with id {0} changes to playfield {1}", obj.id, obj.playfield);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Playfield_Stats:
                        {
                            Eleon.Modding.PlayfieldStats obj = (Eleon.Modding.PlayfieldStats)p.data;
                            if (obj == null) { break; }
                            //addStats(string.Format("Playfield stats for Akua: fps={0} heap={1} procid={2}", obj.fps, obj.mem, obj.processId));
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Playfield_Loaded:
                        {
                            Eleon.Modding.PlayfieldLoad obj = (Eleon.Modding.PlayfieldLoad)p.data;
                            if (obj == null) { break; }

                            Output("Playfield {0} loaded pid={1}", obj.playfield, obj.processId);

                            //lock (playfields)
                            //{
                            //    playfields.Add(obj.playfield);
                            //    System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => mainWindowDataContext.onlinePlayfields.Add(obj.playfield)));
                            //}
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Playfield_Unloaded:
                        {
                            Eleon.Modding.PlayfieldLoad obj = (Eleon.Modding.PlayfieldLoad)p.data;
                            if (obj == null) { break; }

                            Output("Playfield {0} unloaded pid={1}", obj.playfield, obj.processId);

                            //lock (playfields)
                            //{
                            //    playfields.Remove(obj.playfield);
                            //    System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => mainWindowDataContext.onlinePlayfields.Remove(obj.playfield)));
                            //}
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Dedi_Stats:
                        {
                            Eleon.Modding.DediStats obj = (Eleon.Modding.DediStats)p.data;
                            if (obj == null) { break; }
                            //addStats(string.Format("Dedi stats: {0}fps", obj.fps));
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_GlobalStructure_List:
                        {
                            Eleon.Modding.GlobalStructureList obj = (Eleon.Modding.GlobalStructureList)p.data;
                            if (obj == null || obj.globalStructures == null) { break; }
                            Output("Global structures. Count: {0}", obj.globalStructures != null ? obj.globalStructures.Count : 0);

                            if (obj.globalStructures != null)
                            {
                                //System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => mainWindowDataContext.structures.Clear()));

                                foreach (KeyValuePair<string, List<Eleon.Modding.GlobalStructureInfo>> kvp in obj.globalStructures)
                                {
                                    Output("Playfield {0}", kvp.Key);

                                    foreach (Eleon.Modding.GlobalStructureInfo g in kvp.Value)
                                    {
                                        //StructureInfo stI = new StructureInfo();
                                        //stI.FromStructureInfo(g, kvp.Key);

                                        Output("  id={0} name={1} type={2} #blocks={3} #devices={4} playfield={5} pos={6}/{7}/{8}", g.id, g.name, g.type, g.cntBlocks, g.cntDevices, kvp.Key, g.pos.x, g.pos.y, g.pos.z);

                                        //System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => mainWindowDataContext.structures.Add(stI)));
                                    }
                                }
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Playfield_List:
                        {
                            Eleon.Modding.PlayfieldList obj = (Eleon.Modding.PlayfieldList)p.data;
                            if (obj == null || obj.playfields == null) { break; }
                            Output("Playfield list. Count: {0}", obj.playfields != null ? obj.playfields.Count : 0);
                            //System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => mainWindowDataContext.onlinePlayfields.Clear()));

                            //lock (playfields)
                            //{
                            //    playfields.Clear();
                            foreach (string s in obj.playfields)
                            {
                                //System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => mainWindowDataContext.onlinePlayfields.Add(s)));
                                Output("  {0}", s);
                                //playfields.Add(s);
                            }
                            //}
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Faction_Changed:
                        {
                            Eleon.Modding.FactionChangeInfo obj = (Eleon.Modding.FactionChangeInfo)p.data;
                            if (obj == null) { break; }

                            Output("Faction changed entity: {0} faction id: {1} faction {2}", obj.id, obj.factionId, obj.factionGroup);

                            if(obj.id == config.EntityToCapture)
                            {
                                // TODO:
                                //saveState.FactionIdToEntityIds[obj.id] = obj.factionId;
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Statistics:
                        {
                            Eleon.Modding.StatisticsParam obj = (Eleon.Modding.StatisticsParam)p.data;
                            if (obj == null) { break; }

                            Output("Event_Statistics: {0} {1} {2} {3} {4}", obj.type, obj.int1, obj.int2, obj.int3, obj.int4);

                            //CoreRemoved,    int1: Structure id, int2: destryoing entity id, int3: (optional) controlling entity id
                            //CoreAdded,      int1: Structure id, int2: destryoing entity id, int3: (optional) controlling entity id
                            //PlayerDied,     // int1: player entity id, int2: death type (Unknown = 0,Projectile = 1,Explosion = 2,Food = 3,Oxygen = 4,Disease = 5,Drowning = 6,Fall = 7,Suicide = 8), int3: (optional) other entity involved, int4: (optional) other entity CV/SV/HV id
                            //StructOnOff,    int1: structure id, int2: changing entity id, int3: 0 = off, 1 = on
                            //StructDestroyed,// int1: structure id, int2: type (0=wipe, 1=decay)
                        }
                        break;

                    case Eleon.Modding.CmdId.Request_ConsoleCommand:
                        {
                            Eleon.Modding.PString obj = (Eleon.Modding.PString)p.data;
                            if (obj == null) { break; }

                            Output("Request_ConsoleCommand: {0}", obj.pstr);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_ChatMessage:
                        {
                            Eleon.Modding.ChatInfo obj = (Eleon.Modding.ChatInfo)p.data;
                            if (obj == null) { break; }

                            string typeName;
                            switch (obj.type)
                            {
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

                            Output("Chat: Player: {0}, Recepient: {1}, Recepient Faction: {2}, {3}, Message: '{4}'", obj.playerId, obj.recipientEntityId, obj.recipientFactionId, typeName, obj.msg);

                            if (obj.type != 8 && obj.type != 7 && obj.msg == "!MODS")
                            {
                                ChatMessage(k_versionString);
                            }

                            if( obj.msg == "/items")
                            {
                                //var 
                                var itemExInfo = new Eleon.Modding.ItemExchangeInfo(obj.playerId, "Title", "Desc", "Button Text", null);

                                SendRequest(Eleon.Modding.CmdId.Request_Player_ItemExchange, Eleon.Modding.CmdId.Request_Player_ItemExchange, itemExInfo);
                            }
                            else if ( obj.msg == "/gohome")
                            {
                                string Playfield = "Akua2";
                                Eleon.Modding.PVector3 co = new Eleon.Modding.PVector3(0, 150, 0);
                                Eleon.Modding.PVector3 rot = new Eleon.Modding.PVector3(0, 0, 0);
                                SendRequest(
                                    Eleon.Modding.CmdId.Request_Player_ChangePlayerfield,
                                    Eleon.Modding.CmdId.Request_Player_ChangePlayerfield
                                    , new Eleon.Modding.IdPlayfieldPositionRotation(obj.playerId, Playfield, co, rot));
                            }

                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_ItemExchange:
                        {
                            var obj = (Eleon.Modding.ItemExchangeInfo)p.data;
                            if (obj == null) { break; }

                            Output("Event_Player_ItemExchange: Player: {0}", obj.id);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_DisconnectedWaiting:
                        {
                            Eleon.Modding.Id obj = (Eleon.Modding.Id)p.data;
                            if (obj == null) { break; }

                            Output("Event_Player_DisconnectedWaiting: Player: {0}", obj.id);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_AlliancesAll:
                        {
                            Eleon.Modding.AlliancesTable obj = (Eleon.Modding.AlliancesTable)p.data;
                            if (obj == null) { break; }

                            int facId1;
                            int facId2;

                            //Only differences to default alliances are listed (everyone in same Origin is by default allied)
                            foreach (int factionHash in obj.alliances)
                            {
                                facId1 = (factionHash >> 16) & 0xffff;
                                facId2 = factionHash & 0xffff;

                                Output("Alliance difference between faction {0} and faction {1}", facId1, facId2);
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Get_Factions:
                        {
                            Eleon.Modding.FactionInfoList obj = (Eleon.Modding.FactionInfoList)p.data;
                            if (obj == null || obj.factions == null) { break; }
                            Output("Faction list. Count: {0}", obj.factions != null ? obj.factions.Count : 0);
                            foreach (Eleon.Modding.FactionInfo fI in obj.factions)
                            {
                                Output("Id: {0}, Abrev: {1}, Name: {2}, Origin: {3}", fI.factionId, fI.abbrev, fI.name, fI.origin);
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Structure_BlockStatistics:
                        {
                            Eleon.Modding.IdStructureBlockInfo obj = (Eleon.Modding.IdStructureBlockInfo)p.data;
                            if (obj == null || obj.blockStatistics == null) { break; }

                            foreach (KeyValuePair<int, int> blockstat in obj.blockStatistics)
                            {
                                Output("Item {0}: Amount: {1}", blockstat.Key, blockstat.Value);
                            }
                            Output("Block statistic for {0}", obj.id);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_BannedPlayers:
                        {
                            Eleon.Modding.BannedPlayerData obj = (Eleon.Modding.BannedPlayerData)p.data;
                            if (obj == null || obj.BannedPlayers == null) { break; }
                            Output("Banned list. Count: {0}", obj.BannedPlayers != null ? obj.BannedPlayers.Count : 0);
                            foreach (Eleon.Modding.BannedPlayerData.BanEntry ba in obj.BannedPlayers)
                            {
                                Output("Id: {0}, Date: {1}", ba.steam64Id, DateTime.FromBinary(ba.dateTime));
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_TraderNPCItemSold:
                        {
                            Eleon.Modding.TraderNPCItemSoldInfo obj = (Eleon.Modding.TraderNPCItemSoldInfo)p.data;
                            if (obj == null) { break; }
                            Output("Trader NPC item sold info: TraderType: {0}, TraderId: {1}, PlayerId: {2}, StructureId: {3}, Item: {4}, Amount: {5}, Price: {6}", obj.traderType, obj.traderEntityId, obj.playerEntityId, obj.structEntityId, obj.boughtItemId, obj.boughtItemCount, obj.boughtItemPrice);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_GetAndRemoveInventory:
                        {
                            Eleon.Modding.Inventory inv = (Eleon.Modding.Inventory)p.data;
                            if (inv == null) { break; }
                            Output("Got and removed Inventory from player {0}", inv.playerId);
                            if (inv.toolbelt != null)
                            {
                                Output("Toolbelt:");
                                for (int i = 0; inv.toolbelt != null && i < inv.toolbelt.Length; i++)
                                {
                                    Output("  " + inv.toolbelt[i].slotIdx + ". " + inv.toolbelt[i].id + " " + inv.toolbelt[i].count + " " + inv.toolbelt[i].ammo);
                                }
                            }
                            if (inv.bag != null)
                            {
                                Output("Bag:");
                                for (int i = 0; inv.bag != null && i < inv.bag.Length; i++)
                                {
                                    Output("  " + inv.bag[i].slotIdx + ". " + inv.bag[i].id + " " + inv.bag[i].count + " " + inv.bag[i].ammo);
                                }
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Playfield_Entity_List:
                        {
                            Eleon.Modding.PlayfieldEntityList obj = (Eleon.Modding.PlayfieldEntityList)p.data;
                            if (obj == null || obj.entities == null) { break; }
                            Output("Entities. Count: {0}", obj.entities != null ? obj.entities.Count : 0);

                            if (obj.entities != null)
                            {
                                Output("Playfield {0}", obj.playfield);

                                foreach (Eleon.Modding.EntityInfo g in obj.entities)
                                {
                                    //EntityInfo stI = new EntityInfo();
                                    //stI.FromEntityInfo(g, obj.playfield);

                                    Output("  id={0} type={1} playfield={2} pos={3}/{4}/{5}", g.id, g.type, obj.playfield, g.pos.x, g.pos.y, g.pos.z);

                                    //System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => mainWindowDataContext.entities.Add(stI)));
                                }

                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_ConsoleCommand:
                        {
                            Eleon.Modding.ConsoleCommandInfo obj = (Eleon.Modding.ConsoleCommandInfo)p.data;
                            if (obj == null) { break; }
                            Output("Player {0}; Console command: {1} Allowed: {2}", obj.playerEntityId, obj.command, obj.allowed);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_NewEntityId:
                        {
                            Output("New ID: {0}", ((Eleon.Modding.Id)p.data).id);
                            break;
                        }

                    default:
                        Output("(1) Unknown package cmd {0}", p.cmd);
                        break;
                }
            }
            catch (Exception ex)
            {
                Output("Error: {0}", ex.Message);
            }
        }

        private static void SendRequest(Eleon.Modding.CmdId cmdID, Eleon.Modding.CmdId seqNr, object data)
        {
            //if (mainWindowDataContext != null && mainWindowDataContext.EnableOutput_SendRequest)
            {
                Output("SendRequest: Command {0} SeqNr: {1}", cmdID, seqNr);
            }
            _client.Send(cmdID, (ushort)seqNr, data);
        }

        private static void Client_ClientMessages(string s)
        {
            Console.Out.WriteLine("Client_ClientMessages: {0}", s);
        }

        private static void ChatMessage(String msg)
        {
            String command = "SAY '" + msg + "'";
            SendRequest(Eleon.Modding.CmdId.Request_ConsoleCommand, Eleon.Modding.CmdId.Request_InGameMessage_AllPlayers, new Eleon.Modding.PString(command));
            Output("ChatMessage(\"{0}\")", msg);
        }

        private static void Output(String format, params object[] args)
        {
            Console.Out.WriteLine(string.Format("Output: {0}", string.Format(format, args)));
        }
    }
}
