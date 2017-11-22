using SharedCode.ExtensionMethods;
using System;

namespace FactionPlayfieldKicker
{
    class Program
    {
        static readonly string k_versionString = "FactionPlayfieldKicker 0.1 by Mortlath.";

        static SharedCode.GameServerConnection _gameServerConnection;

        static Configuration config;

        static void Main(string[] args)
        {
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "Settings.yaml";

            config = Configuration.GetConfiguration<Configuration>(configFilePath);

            using (_gameServerConnection = new SharedCode.GameServerConnection(k_versionString, config))
            {
                _gameServerConnection.Event_Player_ChangedPlayfield += OnEvent_Player_ChangedPlayfield;

                _gameServerConnection.Connect();

                // wait until the user presses Enter.
                string input = Console.ReadLine();
            }
        }

        private static void OnEvent_Player_ChangedPlayfield(Eleon.Modding.IdPlayfield obj, Eleon.Modding.PlayerInfo oldPlayerInfo)
        {
            bool playfieldIsProtected = config.FactionHomeWorlds.ContainsKey(obj.playfield);

            if (playfieldIsProtected)
            {
                if (oldPlayerInfo.playfield != obj.playfield)
                {
                    int factionIdAllowed = config.FactionHomeWorlds[obj.playfield];

                    // check if player is allowed
                    bool playerIsAllowed = (oldPlayerInfo.factionId == factionIdAllowed);

                    if (!playerIsAllowed)
                    {
                        if (oldPlayerInfo.GetIsPrivileged())
                        {
                            _gameServerConnection.DebugOutput("Privileged player {0} not moved out of playfield", oldPlayerInfo.entityId);
                        }
                        else
                        {
                            _gameServerConnection.DebugOutput("Moving player {0} out of new playfield.", oldPlayerInfo.entityId);

                            // send message to player
                            _gameServerConnection.SendRequest(
                                Eleon.Modding.CmdId.Request_InGameMessage_SinglePlayer,
                                Eleon.Modding.CmdId.Request_InGameMessage_SinglePlayer,
                                new Eleon.Modding.IdMsgPrio(oldPlayerInfo.entityId, config.BootMessage, 1, 100));

                            // teleport them to their last location
                            _gameServerConnection.SendRequest(
                                Eleon.Modding.CmdId.Request_Player_ChangePlayerfield,
                                Eleon.Modding.CmdId.Request_Player_ChangePlayerfield
                                , new Eleon.Modding.IdPlayfieldPositionRotation(oldPlayerInfo.entityId, oldPlayerInfo.playfield, oldPlayerInfo.pos, oldPlayerInfo.rot));
                        }
                    }
                }
                else
                {
                    _gameServerConnection.DebugOutput("Can't move player {0} as last position is the same playfield as this one!", oldPlayerInfo.entityId);
                }
            }
        }
    }
}
