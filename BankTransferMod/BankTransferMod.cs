using EmpyrionModApi;
using EmpyrionModApi.ExtensionMethods;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace BankTransferMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class BankTransferMod : IGameMod
    {
        static readonly string k_versionString = EmpyrionModApi.Helpers.GetVersionString(typeof(BankTransferMod));

        private TraceSource _traceSource = new TraceSource("BankTransferMod");

        public void Start(IGameServerConnection gameServerConnection)
        {
            _traceSource.TraceInformation("Starting up...");
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "BankTransferMod_Settings.yaml";
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
                if (msg.StartsWith(_config.BankTransferCommand))
                {
                    string[] msgPieces = msg.Split(' ');

                    if( msgPieces.Length == 3)
                    {
                        string receivingPlayerName = msgPieces[1];
                        string creditsAmountStr = msgPieces[2];

                        uint creditsAmount = uint.Parse(creditsAmountStr);

                        Player receivingPlayer = _gameServerConnection.GetOnlinePlayerByName(receivingPlayerName);

                        if(receivingPlayer != null)
                        {
                            await TransferCreditsToPlayer(player, receivingPlayer, creditsAmount);
                        }
                        else
                        {
                            // player not found
                            /*await*/ player.SendAlarmMessage("Player '{0}' not found.", receivingPlayerName);
                        }
                    }
                    else
                    {
                        // print usage string
                        await PrintUsageInfo(player);
                    }
                }
            }
            catch (FormatException ex)
            {
                // can't parse credit amount
                _traceSource.TraceEvent(TraceEventType.Error, 1, ex.ToString());
                await PrintUsageInfo(player);
            }
            catch (Exception ex)
            {
                _traceSource.TraceEvent(TraceEventType.Error, 1, ex.ToString());
            }
        }

        private async Task PrintUsageInfo(Player player)
        {
            /*await*/ player.SendAlarmMessage("Usage: '{0} (receiving player name) (amount of credits)' ", _config.BankTransferCommand);
        }


        private async Task TransferCreditsToPlayer(Player sendingPlayer, Player receivingPlayer, uint creditsAmount)
        {
            // check money
            var credits = await sendingPlayer.GetCreditBalance();

            if (credits >= creditsAmount)
            {
                await sendingPlayer.AddCredits(-creditsAmount);
                await receivingPlayer.AddCredits(creditsAmount);

                /*await*/ sendingPlayer.SendAttentionMessage("Sent {0} credits to {1}", creditsAmount, receivingPlayer.Name);
                /*await*/ receivingPlayer.SendAttentionMessage("Received {0} credits from {1}", creditsAmount, sendingPlayer.Name);
            }
            else
            {
                /*await*/ sendingPlayer.SendAlarmMessage("Insufficient funds!");
            }
        }

        private IGameServerConnection _gameServerConnection;
        private Configuration _config;
    }
}
