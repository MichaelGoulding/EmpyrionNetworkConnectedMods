using EmpyrionModApi;
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
        static readonly string k_versionString = EmpyrionModApi.Helpers.GetVersionString(typeof(PlayfieldStructureRegenMod));

        public void Start(IGameServerConnection gameServerConnection)
        {
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "PlayfieldStructureRegenMod_Settings.yaml";

            _gameServerConnection = gameServerConnection;
            _config = BaseConfiguration.GetConfiguration<Configuration>(configFilePath);

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_Playfield_Loaded += OnEvent_Playfield_Loaded;
        }

        public void Stop()
        {
        }

        private void OnEvent_Playfield_Loaded(Playfield playfield)
        {
            if( _config.PlayfieldsToRegenerate.ContainsKey(playfield.Name))
            {
                foreach(var entityId in _config.PlayfieldsToRegenerate[playfield.Name].StructuresIds)
                {
                    playfield.RegenerateStructure(entityId);
                }

                if (_config.PlayfieldsToRegenerate[playfield.Name].RegenerateAllAsteroids)
                {
                    RegenerateAllAsteroids(playfield).Start(); // don't wait for these commands to finish
                }
            }
        }

        private Task RegenerateAllAsteroids(Playfield playfield)
        {
            return Task.WhenAll(
                from structure in playfield.StructuresById.Values
                where (structure.Type == Entity.EntityType.AstVoxel)
                select structure.Regenerate());
        }

        private IGameServerConnection _gameServerConnection;
        private Configuration _config;
    }
}
