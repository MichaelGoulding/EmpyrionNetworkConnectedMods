using SharedCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayfieldStructureRegenMod
{
    public class PlayfieldStructureRegenMod
    {
        static readonly string k_versionString = SharedCode.Helpers.GetVersionString(typeof(PlayfieldStructureRegenMod));

        public PlayfieldStructureRegenMod(GameServerConnection gameServerConnection, Configuration config)
        {
            _gameServerConnection = gameServerConnection;
            _config = config;

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_Playfield_Loaded += OnEvent_Playfield_Loaded; ;
        }

        private void OnEvent_Playfield_Loaded(Playfield playfield)
        {
            if( _config.PlayfieldsToRegenerate.ContainsKey(playfield.Name))
            {
                foreach(var entityId in _config.PlayfieldsToRegenerate[playfield.Name].StructuresIds)
                {
                    playfield.RegenerateStructure(entityId);
                }
            }
        }

        private GameServerConnection _gameServerConnection;
        private Configuration _config;
    }
}
