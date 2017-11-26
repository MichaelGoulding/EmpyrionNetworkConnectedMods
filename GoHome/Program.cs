using EPMConnector;
using System;
using System.Collections.Generic;

namespace GoHome
{
    class Program
    {
        static readonly string k_versionString = "GoHome 0.2 by Mortlath.";

        static EPMConnector.Client _client = new EPMConnector.Client(12345);

        static Config.Configuration config;

        static Dictionary<int, Eleon.Modding.PlayerInfo> playerInfoById = new Dictionary<int, Eleon.Modding.PlayerInfo>();

        static void Main(string[] args)
        {
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "Settings.yaml";

            config = Config.Configuration.GetConfiguration(configFilePath);

            // connect to server
            _client.GameEventReceived += Client_GameEventReceived;
            _client.ClientMessages += (string s) => { Console.Out.WriteLine("Client_ClientMessages: {0}", s); };

            _client.Connect(config.GameServerIp, config.GameServerApiPort);

            // Request player list to start off
            SendRequest(Eleon.Modding.CmdId.Request_Player_List, Eleon.Modding.CmdId.Request_Player_List, null);

            // wait until the user presses Enter.
            string input = Console.ReadLine();

            _client.Disconnect();

            _client = null;
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

                switch (p.cmd)
                {
                    case Eleon.Modding.CmdId.Event_Player_Connected:
                        {
                            int entityId = ((Eleon.Modding.Id)p.data).id;

                            Output("Player with id {0} connected", entityId);

                            SendRequest(Eleon.Modding.CmdId.Request_Player_Info, Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(entityId));
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_List:
                        {
                            if (p.data != null)
                            {
                                var playerIds = ((Eleon.Modding.IdList)p.data).list;

                                for (int i = 0; i < playerIds.Count; i++)
                                {
                                    SendRequest(Eleon.Modding.CmdId.Request_Player_Info, Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(playerIds[i]));
                                    Output("{0} Player with id {1}", i + 1, playerIds[i]);
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
                            lock(playerInfoById)
                            {
                                playerInfoById[pInfo.entityId] = pInfo;
                            }
                            Output("Player info (seqnr {0}): cid={1} eid={2} name={3} playfield={4} fac={5}", p.seqNr, pInfo.clientId, pInfo.entityId, pInfo.playerName, pInfo.playfield, pInfo.factionId);
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_ChatMessage:
                        {
                            Eleon.Modding.ChatInfo obj = (Eleon.Modding.ChatInfo)p.data;
                            if (obj == null) { break; }

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

                            Output("Chat: Player: {0}, Recepient: {1}, Recepient Faction: {2}, {3}, Message: '{4}'", obj.playerId, obj.recipientEntityId, obj.recipientFactionId, typeName, obj.msg);

                            if (obj.type != 8 && obj.type != 7 && obj.msg == "!MODS")
                            {
                                ChatMessage(k_versionString);
                            }

                            if (obj.msg ==config.TeleportCommand || obj.msg == string.Format("s! {0}", config.TeleportCommand))
                            {
                                lock (playerInfoById)
                                {
                                    Config.Configuration.FactionHomeWorldData homeworldData = null;

                                    if (config.FactionHomeWorlds.ContainsKey(playerInfoById[obj.playerId].factionId))
                                    {
                                        homeworldData = config.FactionHomeWorlds[playerInfoById[obj.playerId].factionId];
                                    }
                                    else if(config.FactionHomeWorlds.ContainsKey(-1))
                                    {
                                        // everyone else goes to the default
                                        homeworldData = config.FactionHomeWorlds[-1];
                                    }

                                    if (homeworldData != null)
                                    {
                                        if((homeworldData.FromPlayfield == null) || homeworldData.FromPlayfield == playerInfoById[obj.playerId].playfield)
                                        {
                                            var location = homeworldData.GetNextLocation();
                                            SendRequest(
                                                Eleon.Modding.CmdId.Request_Player_ChangePlayerfield,
                                                Eleon.Modding.CmdId.Request_Player_ChangePlayerfield
                                                , new Eleon.Modding.IdPlayfieldPositionRotation(obj.playerId, homeworldData.ToPlayfield, location.Position, location.Rotation));
                                        }
                                        else
                                        {
                                            // TODO: send failure message to player
                                        }
                                    }
                                    else
                                    {
                                        // TODO: send failure message to player
                                    }
                                }


                            }

                        }
                        break;

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
            Output("SendRequest: Command {0} SeqNr: {1}", cmdID, seqNr);
            _client.Send(cmdID, (ushort)seqNr, data);
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
