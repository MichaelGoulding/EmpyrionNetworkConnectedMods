﻿using SharedCode.ExtensionMethods;
using System;
using System.Linq;

namespace FactionPlayfieldKicker
{
    class Program
    {
        static readonly string k_versionString = SharedCode.VersionHelper.GetVersionString(typeof(Program));

        static SharedCode.GameServerConnection _gameServerConnection;

        static Configuration config;

        static void Main(string[] args)
        {
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "Settings.yaml";

            config = Configuration.GetConfiguration<Configuration>(configFilePath);

            using (_gameServerConnection = new SharedCode.GameServerConnection(config))
            {
                _gameServerConnection.AddVersionString(k_versionString);
                _gameServerConnection.Event_Player_ChangedPlayfield += OnEvent_Player_ChangedPlayfield;

                _gameServerConnection.Connect();

                // wait until the user presses Enter.
                string input = Console.ReadLine();
            }
        }

        private static void OnEvent_Player_ChangedPlayfield(SharedCode.Playfield newPlayfield, SharedCode.Player oldPlayerInfo)
        {
            bool playfieldIsProtected = config.FactionHomeWorlds.ContainsKey(newPlayfield.Name);

            if (playfieldIsProtected)
            {
                if (oldPlayerInfo.Position.playfield != newPlayfield)
                {
                    int factionIdAllowed = config.FactionHomeWorlds[newPlayfield.Name];

                    // check if player is allowed
                    bool playerIsAllowed = (oldPlayerInfo.MemberOfFaction == factionIdAllowed);

                    if (!playerIsAllowed)
                    {
                        if (oldPlayerInfo.IsPrivileged)
                        {
                            _gameServerConnection.DebugOutput("Privileged player {0} not moved out of playfield", oldPlayerInfo);
                        }
                        else
                        {
                            _gameServerConnection.DebugOutput("Moving player {0} out of new playfield.", oldPlayerInfo);

                            oldPlayerInfo.SendAlarmMessage(config.BootMessage);

                            // teleport them to their last location
                            oldPlayerInfo.ChangePlayerfield(oldPlayerInfo.Position);
                        }
                    }
                }
                else
                {
                    _gameServerConnection.DebugOutput("Can't move player {0} as last position is the same playfield as this one!", oldPlayerInfo);
                }
            }
        }
    }
}
