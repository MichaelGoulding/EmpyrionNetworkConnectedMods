using SharedCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactionPlayfieldKickerMod
{
    public class FactionPlayfieldKickerMod
    {
        static readonly string k_versionString = VersionHelper.GetVersionString(typeof(FactionPlayfieldKickerMod));

        public FactionPlayfieldKickerMod(GameServerConnection gameServerConnection, Configuration config)
        {
            _gameServerConnection = gameServerConnection;
            _config = config;

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_Player_ChangedPlayfield += OnEvent_Player_ChangedPlayfield;
        }

        private void OnEvent_Player_ChangedPlayfield(SharedCode.Playfield newPlayfield, SharedCode.Player oldPlayerInfo)
        {
            bool playfieldIsProtected = _config.FactionHomeWorlds.ContainsKey(newPlayfield.Name);

            if (playfieldIsProtected)
            {
                if (oldPlayerInfo.Position.playfield != newPlayfield)
                {
                    int factionIdAllowed = _config.FactionHomeWorlds[newPlayfield.Name];

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

                            oldPlayerInfo.SendAlarmMessage(_config.BootMessage);

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

        private GameServerConnection _gameServerConnection;
        private Configuration _config;
    }
}
