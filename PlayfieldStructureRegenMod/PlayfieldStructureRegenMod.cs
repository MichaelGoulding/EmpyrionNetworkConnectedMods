using SharedCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayfieldStructureRegenMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class PlayfieldStructureRegenMod : IGameMod
    {
        static readonly string k_versionString = SharedCode.Helpers.GetVersionString(typeof(PlayfieldStructureRegenMod));

        public void Start(IGameServerConnection gameServerConnection)
        {
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "PlayfieldStructureRegenMod_Settings.yaml";

            _gameServerConnection = gameServerConnection;
            _config = SharedCode.BaseConfiguration.GetConfiguration<Configuration>(configFilePath);

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_Playfield_Loaded += OnEvent_Playfield_Loaded;
            _gameServerConnection.Event_ChatMessage += OnEvent_ChatMessage;
        }

        public void Stop()
        {
        }

        private void OnEvent_ChatMessage(ChatType chatType, string msg, Player player)
        {
            //if (msg.StartsWith("/reg"))
            //{
            //    var strings = msg.Split(' ');

            //    int entityId = int.Parse(strings[1]);

            //    _gameServerConnection.SendRequest(
            //        Eleon.Modding.CmdId.Request_ConsoleCommand,
            //        new Eleon.Modding.PString(string.Format("remoteex pf={0} 'regenerate {1}'", player.Position.playfield.ProcessId, entityId)));
            //}
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

        private IGameServerConnection _gameServerConnection;
        private Configuration _config;
    }
}
