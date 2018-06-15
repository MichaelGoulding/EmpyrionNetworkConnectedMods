using EmpyrionModApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactionPlayfieldKickerMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class FactionPlayfieldKickerMod: IGameMod
    {
        static readonly string k_versionString = Helpers.GetVersionString(typeof(FactionPlayfieldKickerMod));

        public void Start(IGameServerConnection gameServerConnection)
        {
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "FactionPlayfieldKickerMod_Settings.yaml";

            _gameServerConnection = gameServerConnection;
            _config = BaseConfiguration.GetConfiguration<Configuration>(configFilePath);

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_Player_ChangedPlayfield += OnEvent_Player_ChangedPlayfield;
        }

        public void Stop()
        {
        }

        private void OnEvent_Player_ChangedPlayfield(Playfield newPlayfield, Player oldPlayerInfo)
        {
            bool playfieldIsProtected = _config.FactionHomeWorlds.ContainsKey(newPlayfield.Name);

            if (playfieldIsProtected)
            {
                if (oldPlayerInfo.Position.playfield != newPlayfield)
                {
                    int factionIdAllowed = _config.FactionHomeWorlds[newPlayfield.Name];

                    // check if player is allowed
                    bool playerIsAllowed = (oldPlayerInfo.FactionIdOrEntityId == factionIdAllowed);

                    if (!playerIsAllowed)
                    {
                        if (oldPlayerInfo.IsPrivileged)
                        {
                            _gameServerConnection.DebugOutput("Privileged player {0} not moved out of playfield", oldPlayerInfo);
                        }
                        else
                        {
                            _gameServerConnection.DebugOutput("Moving player {0} out of new playfield.", oldPlayerInfo);

                            oldPlayerInfo.SendAlarmMessage(_config.BootMessage);

                            // teleport them to their last location
                            oldPlayerInfo.ChangePlayfield(oldPlayerInfo.Position);
                        }
                    }
                }
                else
                {
                    _gameServerConnection.DebugOutput("Can't move player {0} as last position is the same playfield as this one!", oldPlayerInfo);
                }
            }
        }

        private IGameServerConnection _gameServerConnection;
        private Configuration _config;
    }
}
