using EmpyrionModApi;
using EmpyrionModApi.ExtensionMethods;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace RadarMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class RadarMod : IGameMod
    {
        static readonly string k_versionString = EmpyrionModApi.Helpers.GetVersionString(typeof(RadarMod));

        private TraceSource _traceSource = new TraceSource("BankTransferMod");

        public void Start(IGameServerConnection gameServerConnection)
        {
            _traceSource.TraceInformation("Starting up...");
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "RadarMod_Settings.yaml";
            _traceSource.TraceEvent(TraceEventType.Verbose, 3, "Loaded configuration.");

            _gameServerConnection = gameServerConnection;
            _config = BaseConfiguration.GetConfiguration<Configuration>(configFilePath);

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_ChatMessage += OnEvent_ChatMessage;
        }

        public void Stop()
        {
            _traceSource.TraceInformation("Stopping...");
            _traceSource.Flush();
            _traceSource.Close();
        }

        private async void OnEvent_ChatMessage(ChatType chatType, string msg, Player player)
        {
            try
            {
                if (msg.StartsWith(_config.SectorScanCommand))
                {
                    await OnSectorScanCommand(player);
                }
            }
            catch (Exception ex)
            {
                _traceSource.TraceEvent(TraceEventType.Error, 1, ex.ToString());
            }
        }

        private async Task OnSectorScanCommand(Player player)
        {
            if (_config.AnnounceSectorScan)
            {
                await _gameServerConnection.SendChatMessageToAll(
                    "Player {0} is scanning sector {1} for any online players.",
                    player.Name,
                    player.Position.playfield.Name);
            }

            var playersInPlayfield = _gameServerConnection.GetOnlinePlayersByPlayfield(player.Position.playfield);

            if (playersInPlayfield.Count > 1)
            {
                await player.SendChatMessage("Players in sector:");

                foreach (var otherPlayer in playersInPlayfield)
                {
                    await player.SendChatMessage("   {0}", otherPlayer.Name);
                }
            }
            else
            {
                await player.SendChatMessage("No one else in the sector than you.");
            }
        }

        private IGameServerConnection _gameServerConnection;
        private Configuration _config;
    }
}
