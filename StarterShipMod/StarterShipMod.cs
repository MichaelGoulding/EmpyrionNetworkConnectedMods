using EmpyrionModApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarterShipMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class StarterShipMod : IGameMod
    {
        static readonly string k_versionString = EmpyrionModApi.Helpers.GetVersionString(typeof(StarterShipMod));

        static readonly string k_saveStateFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "SaveState.yaml";

        private TraceSource _traceSource = new TraceSource("StarterShipMod");
        private IGameServerConnection _gameServerConnection;
        private Configuration _config;
        private SaveState _saveState;

        public void Start(IGameServerConnection gameServerConnection)
        {
            _traceSource.TraceInformation("Starting up...");
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "StarterShipMod_Settings.yaml";

            _gameServerConnection = gameServerConnection;
            _config = BaseConfiguration.GetConfiguration<Configuration>(configFilePath);

            _saveState = SaveState.Load(k_saveStateFilePath);

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_ChatMessage += OnEvent_ChatMessage;
        }

        public void Stop()
        {
            lock (_saveState)
            {
                _saveState.Save(k_saveStateFilePath);
            }

            _traceSource.TraceInformation("Stopping...");
            _traceSource.Flush();
            _traceSource.Close();
        }

        private void OnEvent_ChatMessage(ChatType chatType, string msg, Player player)
        {
            if (msg == _config.StarterShipCommand)
            {
                _traceSource.TraceInformation($"Player '{player}' asked for a ship.");
                lock (_saveState)
                {
                    if (_saveState.HasGotStarterShip(player))
                    {
                        _traceSource.TraceInformation($"Player '{player}' already redeemed a ship.");
                        player.SendAlarmMessage("You already redeemed your starter ship earlier.");
                    }
                    else
                    {
                        _traceSource.TraceInformation($"Creating '{_config.BlueprintName}' for '{player}'.");

                        player.GetCurrentPosition().ContinueWith((posTask) =>
                        {
                            player.Position.playfield.SpawnEntity(
                                string.Format(_config.ShipNameFormat, player.Name),
                                Entity.EntityType.SV, // this value works for CVs as well.
                                _config.BlueprintName,
                                player.Position.position + new System.Numerics.Vector3(0, 50, 0),
                                player)
                                .ContinueWith(
                                    (task) =>
                                    {
                                        player.SendAlertMessage("Look up.");
                                        lock (_saveState)
                                        {
                                            _traceSource.TraceInformation($"Recording that '{player}' redeemed a ship.");
                                            _saveState.MarkGotStarterShip(player);
                                            _saveState.Save(k_saveStateFilePath);
                                        }
                                    });
                        });
                    }
                }
            }
        }
    }
}
