using EmpyrionModApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarterCreditsMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class StarterCreditsMod : IGameMod
    {
        static readonly string k_versionString = EmpyrionModApi.Helpers.GetVersionString(typeof(StarterCreditsMod));

        static readonly string k_saveStateFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "SaveState.yaml";

        private TraceSource _traceSource = new TraceSource("StarterCreditsMod");
        private IGameServerConnection _gameServerConnection;
        private Configuration _config;
        private SaveState _saveState;

        public void Start(IGameServerConnection gameServerConnection)
        {
            _traceSource.TraceInformation("Starting up...");
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "StarterCreditsMod_Settings.yaml";

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

        private async void OnEvent_ChatMessage(ChatType chatType, string msg, Player player)
        {
            if (msg == _config.StarterCreditsCommand)
            {
                _traceSource.TraceInformation($"Player '{player}' asked for starter credits.");

                bool hasGotStarterShip;
                lock (_saveState)
                {
                    hasGotStarterShip = _saveState.HasGotStarterCredits(player);
                }

                if (hasGotStarterShip)
                {
                    _traceSource.TraceInformation($"Player '{player}' already redeemed their starter credits.");
                    await player.SendAlarmMessage("You already redeemed your starter credits earlier.");
                }
                else
                {
                    await OnGetStarterCredits(player);
                }
            }
        }

        private static int GetLevelAsInt(ExpLevel playersLevel)
        {
            int index = 1;
            foreach (ExpLevel level in System.Enum.GetValues(typeof(ExpLevel)))
            {
                if(playersLevel == level)
                {
                    return index;
                }
                else
                {
                    ++index;
                }
            }

            throw new InvalidOperationException(playersLevel.ToString());
        }

        private async Task OnGetStarterCredits(Player player)
        {
            var playersLevel = await player.GetExperienceLevel();
            if (playersLevel < _config.MinimumLevelNeeded)
            {
                _traceSource.TraceInformation($"Player '{player}' not the right level.  Has {playersLevel}, but needs {_config.MinimumLevelNeeded}.");
                await player.SendAlarmMessage($"You need to be at least level {GetLevelAsInt(_config.MinimumLevelNeeded)} to redeem your starter credits.");
            }
            else
            {
                _traceSource.TraceInformation($"Giving '{_config.AmountOfCredits}' to '{player}'.");

                await player.AddCredits(_config.AmountOfCredits);

                 lock (_saveState)
                {
                    _traceSource.TraceInformation($"Recording that '{player}' redeemed credits.");
                    _saveState.MarkGotStarterCredits(player);
                    _saveState.Save(k_saveStateFilePath);
                }

                await player.SendAlertMessage($"{_config.AmountOfCredits} credits redeemed.");
            }
        }
    }
}
