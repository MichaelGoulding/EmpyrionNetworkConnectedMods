using EmpyrionModApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerPlaytimeRewardsMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class ServerPlaytimeRewardsMod : IGameMod
    {
        static readonly string k_versionString = EmpyrionModApi.Helpers.GetVersionString(typeof(ServerPlaytimeRewardsMod));

        private TraceSource _traceSource = new TraceSource("ServerPlaytimeRewardsMod");
        private IGameServerConnection _gameServerConnection;
        private Configuration _config;


        public void Start(IGameServerConnection gameServerConnection)
        {
            _traceSource.TraceInformation("Starting up...");
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "ServerPlaytimeRewardsMod_Settings.yaml";

            _gameServerConnection = gameServerConnection;
            _config = BaseConfiguration.GetConfiguration<Configuration>(configFilePath);

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_ChatMessage += OnEvent_ChatMessage;
            _gameServerConnection.Event_PlayerDied += OnEvent_PlayerDied;
        }

        public void Stop()
        {
            _traceSource.TraceInformation("Stopping...");
            _traceSource.Flush();
            _traceSource.Close();
        }

        private void OnEvent_ChatMessage(ChatType chatType, string msg, Player player)
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
                        JailPlayer(requesterName, playerToJail, reason)
                            .ContinueWith((task) =>
                            {
                                player.SendAttentionMessage($"{playerNameToJail} jailed for \"{reason}\".");

                            });
                    }
                    else
                    {
                        player.SendAlarmMessage($"{playerNameToJail} is not found.");
                    }
                }
                else
                {
                    Match jailExitMatch = _jailExitRegex.Match(msg);
                    if (jailExitMatch.Success)
                    {
                        string playerNameToRelease = jailExitMatch.Groups[1].Value;

                        Player playerToRelease = _gameServerConnection.GetOnlinePlayerByName(playerNameToRelease);

                        if (playerToRelease != null)
                        {
                            playerToRelease.ChangePlayfield(_jailExitLocation)
                                .ContinueWith((task) =>
                                {
                                    player.SendAttentionMessage($"{playerToRelease} released.");
                                    playerToRelease.SendAttentionMessage($"{requesterName} freed you.");
                                });
                        }
                        else
                        {
                            player.SendAlarmMessage($"{playerNameToRelease} is not found.");
                        }
                    }
                }
            }
        }
    }
}

