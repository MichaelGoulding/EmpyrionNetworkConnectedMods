using EmpyrionModApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ServerPlaytimeRewardsMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class ServerPlaytimeRewardsMod : IGameMod
    {
        static readonly string k_versionString = EmpyrionModApi.Helpers.GetVersionString(typeof(ServerPlaytimeRewardsMod));

        private TraceSource _traceSource = new TraceSource("ServerPlaytimeRewardsMod");
        private IGameServerConnection _gameServerConnection;
        private Configuration _config;

        private Dictionary<Player, Timer> _playerRewardTimers = new Dictionary<Player, Timer>();

        public void Start(IGameServerConnection gameServerConnection)
        {
            _traceSource.TraceInformation("Starting up...");
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "ServerPlaytimeRewardsMod_Settings.yaml";

            _gameServerConnection = gameServerConnection;
            _config = BaseConfiguration.GetConfiguration<Configuration>(configFilePath);

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_Player_Connected += OnEvent_Player_Connected;
            _gameServerConnection.Event_Player_Disconnected += OnEvent_Player_Disconnected;
        }

        public void Stop()
        {
            _traceSource.TraceInformation("Stopping...");
            foreach (var timer in _playerRewardTimers.Values)
            {
                timer.Stop();
            }
            _traceSource.Flush();
            _traceSource.Close();
        }


        private void OnEvent_Player_Connected(Player player)
        {
             var timer = new Timer(_config.XpRewardPeriodInMinutes * 1000.0 * 60.0);

            timer.Elapsed += (object sender, ElapsedEventArgs e) => { OnPlayerRewardTimer_Elapsed(player); };

            _playerRewardTimers[player] = timer;

            _traceSource.TraceInformation($"Starting reward timer for {player} who just connected.");

            timer.Start();
        }

        private async void OnPlayerRewardTimer_Elapsed(Player player)
        {
            var level = await player.GetExperienceLevel();

            if (level < ExpLevel.L25)
            {
                _traceSource.TraceInformation($"Giving {player} {_config.XpPerPeriod} xp points.");
                await player.ChangeExperiencePoints(_config.XpPerPeriod);
            }
        }

        private void OnEvent_Player_Disconnected(Player player)
        {
            if (_playerRewardTimers.ContainsKey(player))
            {
                _traceSource.TraceInformation($"Stopping reward timer for {player} who just disconnected.");
                _playerRewardTimers[player].Stop();
                _playerRewardTimers.Remove(player);
            }
        }
    }
}

