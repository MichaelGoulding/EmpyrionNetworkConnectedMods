using EmpyrionModApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NoKillZonesMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class NoKillZonesMod : IGameMod
    {
        static readonly string k_versionString = EmpyrionModApi.Helpers.GetVersionString(typeof(NoKillZonesMod));

        static readonly string k_saveStateFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "SaveState.yaml";

        private TraceSource _traceSource = new TraceSource("NoKillZonesMod");
        private IGameServerConnection _gameServerConnection;
        private Configuration _config;
        private SaveState _saveState;
        private WorldPosition _jailLocation;
        private WorldPosition _jailExitLocation;

        private Regex _jailRequestRegex = new Regex("/jail (.+) \"(.*)\"");
        private Regex _jailExitRegex = new Regex("/free (.+)");


        public void Start(IGameServerConnection gameServerConnection)
        {
            _traceSource.TraceInformation("Starting up...");
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "NoKillZonesMod_Settings.yaml";

            _gameServerConnection = gameServerConnection;
            _config = BaseConfiguration.GetConfiguration<Configuration>(configFilePath);
            _jailLocation = new WorldPosition( _gameServerConnection, _config.JailLocation);
            _jailExitLocation = new WorldPosition(_gameServerConnection, _config.JailExitLocation);

            _saveState = SaveState.Load(k_saveStateFilePath);

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_ChatMessage += OnEvent_ChatMessage;
            _gameServerConnection.Event_PlayerDied += OnEvent_PlayerDied;
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
            if (player.IsPrivileged)
            {
                string requesterName = player.Name;

                Match jailRequestMatch = _jailRequestRegex.Match(msg);
                if (jailRequestMatch.Success)
                {
                    string playerNameToJail = jailRequestMatch.Groups[1].Value;
                    string reason = jailRequestMatch.Groups[2].Value;

                    Player playerToJail = _gameServerConnection.GetOnlinePlayerByName(playerNameToJail);

                    if (playerToJail != null)
                    {
                        await JailPlayer(requesterName, playerToJail, reason);
                        await player.SendAttentionMessage($"{playerNameToJail} jailed for \"{reason}\".");
                    }
                    else
                    {
                        await player.SendAlarmMessage($"{playerNameToJail} is not found.");
                    }
                }
                else
                {
                    Match jailExitMatch = _jailExitRegex.Match(msg);
                    if( jailExitMatch.Success)
                    {
                        string playerNameToRelease = jailExitMatch.Groups[1].Value;

                        Player playerToRelease = _gameServerConnection.GetOnlinePlayerByName(playerNameToRelease);

                        if (playerToRelease != null)
                        {
                            await playerToRelease.ChangePlayfield(_jailExitLocation);
                            await player.SendAttentionMessage($"{playerToRelease} released.");
                            await playerToRelease.SendAttentionMessage($"{requesterName} freed you.");
                        }
                        else
                        {
                            await player.SendAlarmMessage($"{playerNameToRelease} is not found.");
                        }
                    }
                }
            }
        }

        private async Task JailPlayer(string requesterName, Player playerToJail, string reason)
        {
            _traceSource.TraceInformation($"{requesterName} asked for {playerToJail} to be jailed for \"{reason}\"!");

            await playerToJail.ChangePlayfield(_jailLocation);

            await playerToJail.SendAlarmMessage($"{requesterName} jailed you for \"{reason}\".");
        }

        private async void OnEvent_PlayerDied(Player deadPlayer, PlayerDeathInfo playerDeathInfo)
        {
            var deathLocation = await deadPlayer.GetCurrentPosition();

            _traceSource.TraceInformation("Player {0} died at {1}", deadPlayer, deathLocation);

            if (playerDeathInfo.killer != null)
            {
                Player killer = playerDeathInfo.killer;

                var noKillZone = GetNoKillZoneAtPlayerLocation(deathLocation);

                if (noKillZone != null)
                {
                    _traceSource.TraceInformation($"Rule breaker player {killer}!!");

                    await _gameServerConnection.SendMessageToAll(MessagePriority.Attention, 20*1000, $"{killer.Name} broke the rules and killed {deadPlayer.Name} in the {noKillZone.Name}.  He or she will be jailed.");

                    await JailPlayer("The server", killer, $"For breaking the rules and killing {deadPlayer.Name}");
                }
            }
        }

        private Configuration.NoKillZone GetNoKillZoneAtPlayerLocation(WorldPosition playerPosition)
        {
            foreach (var noKillZone in _config.NoKillZones)
            {
                BoundingBox boundingBox = new BoundingBox(_gameServerConnection, noKillZone.BoundingBox);

                if (boundingBox.IsInside(playerPosition))
                {
                    return noKillZone;
                }
            }

            return null;
        }
    }
}
