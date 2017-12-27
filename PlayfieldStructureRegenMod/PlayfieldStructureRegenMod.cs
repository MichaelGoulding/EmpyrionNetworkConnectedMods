using EmpyrionModApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayfieldStructureRegenMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class PlayfieldStructureRegenMod : IGameMod
    {
        static readonly string k_versionString = EmpyrionModApi.Helpers.GetVersionString(typeof(PlayfieldStructureRegenMod));

        private TraceSource _traceSource = new TraceSource("PlayfieldStructureRegenMod");

        public void Start(IGameServerConnection gameServerConnection)
        {
            _traceSource.TraceInformation("Starting up...");
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "PlayfieldStructureRegenMod_Settings.yaml";

            _gameServerConnection = gameServerConnection;
            _config = BaseConfiguration.GetConfiguration<Configuration>(configFilePath);

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_Playfield_Loaded += OnEvent_Playfield_Loaded;
        }

        public void Stop()
        {
            _traceSource.TraceInformation("Stopping...");
            _traceSource.Flush();
            _traceSource.Close();
        }

        private void OnEvent_Playfield_Loaded(Playfield playfield)
        {
            try
            {
                if (_config.PlayfieldsToRegenerate.ContainsKey(playfield.Name))
                {
                    foreach (var entityId in _config.PlayfieldsToRegenerate[playfield.Name].StructuresIds)
                    {
                        playfield.RegenerateStructure(entityId);
                    }

                    if (_config.PlayfieldsToRegenerate[playfield.Name].RegenerateAllAsteroids)
                    {
                        RegenerateAllAsteroids(playfield)
                            .ContinueWith(
                            (task) =>
                            {
                                _traceSource.TraceEvent(TraceEventType.Error, 1, task.Exception.ToString());
                            }, TaskContinuationOptions.OnlyOnFaulted); // don't wait for these commands to finish
                    }
                }
            }
            catch(Exception ex)
            {
                _traceSource.TraceEvent(TraceEventType.Error, 1, ex.ToString());
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
