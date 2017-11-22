using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

using PlayerInfo = Eleon.Modding.PlayerInfo;

namespace SharedCode
{
    public class GameServerConnection : IDisposable
    {
        #region Private Data

        readonly string k_versionString;

        EPMConnector.Client _client = new EPMConnector.Client();

        Dictionary<int, PlayerInfo> _playerInfoById = new Dictionary<int, PlayerInfo>();
        Timer _playerUpdateTimer;

        BaseConfiguration _config;

        #endregion

        public event Action<Eleon.Modding.IdPlayfield, PlayerInfo> Event_Player_ChangedPlayfield;

        public GameServerConnection( string versionString, BaseConfiguration config )
        {
            _config = config;
            k_versionString = versionString;
            _playerUpdateTimer = new Timer(config.PlayerUpdateIntervalInSeconds * 1000);
            _playerUpdateTimer.Elapsed += OnPlayerUpdateTimer_Elapsed;

            _client.GameEventReceived += Client_GameEventReceived;
            _client.ClientMessages += (string s) => { Console.Out.WriteLine("Client_ClientMessages: {0}", s); };
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
            DebugOutput("SendRequest: Command {0} SeqNr: {1}", cmdID, seqNr);
            _client.Send(cmdID, (ushort)seqNr, data);
        }

        public void ChatMessage(String msg)
        {
            String command = "SAY '" + msg + "'";
            SendRequest(Eleon.Modding.CmdId.Request_ConsoleCommand, Eleon.Modding.CmdId.Request_InGameMessage_AllPlayers, new Eleon.Modding.PString(command));
            DebugOutput("ChatMessage(\"{0}\")", msg);
        }

        public PlayerInfo GetPlayerInfoById(int entityId)
        {
            lock (_playerInfoById)
            {
                return _playerInfoById[entityId];
            }
        }

        private void Client_GameEventReceived(EPMConnector.ModProtocol.Package p)
        {
            try
            {
                if (p.data == null)
                {
                    DebugOutput("Empty Package id rec: {0}", p.cmd);
                    return;
                }

                switch (p.cmd)
                {
                    case Eleon.Modding.CmdId.Event_Player_Connected:
                        {
                            int entityId = ((Eleon.Modding.Id)p.data).id;

                            DebugOutput("Player with id {0} connected", entityId);

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
                                    DebugOutput("{0} Player with id {1}", i + 1, playerIds[i]);
                                }
                            }
                            else
                            {
                                DebugOutput("No players connected");
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_Info:
                        {
                            Eleon.Modding.PlayerInfo pInfo = (Eleon.Modding.PlayerInfo)p.data;
                            if (pInfo == null) { break; }
                            DebugOutput("Player info (seqnr {0}): cid={1} eid={2} name={3} playfield={4} fac={5}", p.seqNr, pInfo.clientId, pInfo.entityId, pInfo.playerName, pInfo.playfield, pInfo.factionId);

                            lock (_playerInfoById)
                            {
                                _playerInfoById[pInfo.entityId] = pInfo;
                            }

                            if (!_playerUpdateTimer.Enabled)
                            {
                                _playerUpdateTimer.Start();
                            }
                        }
                        break;

                    case Eleon.Modding.CmdId.Event_Player_ChangedPlayfield:
                        {
                            Eleon.Modding.IdPlayfield obj = (Eleon.Modding.IdPlayfield)p.data;

                            DebugOutput("Player with id {0} changes to playfield {1}", obj.id, obj.playfield);

                            lock (_playerInfoById)
                            {
                                if (_playerInfoById.ContainsKey(obj.id))
                                {
                                    var playerInfo = _playerInfoById[obj.id];

                                    Event_Player_ChangedPlayfield?.Invoke(obj, playerInfo);

                                    // update data
                                    playerInfo.playfield = obj.playfield;
                                }
                            }
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

                            DebugOutput("Chat: Player: {0}, Recepient: {1}, Recepient Faction: {2}, {3}, Message: '{4}'", obj.playerId, obj.recipientEntityId, obj.recipientFactionId, typeName, obj.msg);

                            if (obj.type != 8 && obj.type != 7 && obj.msg == "!MODS")
                            {
                                ChatMessage(k_versionString);
                            }
                        }
                        break;

                    default:
                        DebugOutput("(1) Unknown package cmd {0}", p.cmd);
                        break;
                }
            }
            catch (Exception ex)
            {
                DebugOutput("Error: {0}", ex.Message);
            }
        }

        private void OnPlayerUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_playerInfoById)
            {
                foreach (var entityId in _playerInfoById.Keys)
                {
                    SendRequest(Eleon.Modding.CmdId.Request_Player_Info, Eleon.Modding.CmdId.Request_Player_Info, new Eleon.Modding.Id(entityId));
                }
            }
        }

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
