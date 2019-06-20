using EmpyrionModApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShipBuyingMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class ShipBuyingMod : IGameMod
    {
        static readonly string k_versionString = EmpyrionModApi.Helpers.GetVersionString(typeof(ShipBuyingMod));

        static readonly int k_noOrigin = -1;

        private TraceSource _traceSource = new TraceSource("ShipBuyingMod");
        private IGameServerConnection _gameServerConnection;
        private Configuration _config;

        public void Start(IGameServerConnection gameServerConnection)
        {
            _traceSource.TraceInformation("Starting up...");
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "ShipBuyingMod_Settings.yaml";

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

        private void OnEvent_ChatMessage(ChatType chatType, string msg, Player player)
        {
            if (msg.StartsWith(_config.BuyShipCommand))
            {
                _traceSource.TraceInformation($"Player '{player}' asked to buy a ship.");
                ProcessBuyCommand(msg.Substring(_config.BuyShipCommand.Length), player);
            }
        }

        private async void CompleteTransaction(Player player, Configuration.ShipSeller.ShipInfo shipInfo)
        {
            // check money
            var credits = await player.GetCreditBalance();

            if(credits >= shipInfo.Price)
            {
                // deduct money
                await player.AddCredits(-shipInfo.Price);

                string shipName = string.Format(shipInfo.ShipNameFormat, player.Name);

                // make sure factionlist is up-to-date for player
                await _gameServerConnection.RefreshFactionList();

                var playerPosition = await player.GetCurrentPosition();

                // spawn ship
                await playerPosition.playfield.SpawnEntity(
                    shipName,
                    shipInfo.EntityType,
                    shipInfo.BlueprintName,
                    shipInfo.SpawnLocation.ToNumericsVector3(),
                    player);

                _traceSource.TraceInformation($"Player '{player}' bought {shipInfo.BlueprintName} named '{shipName}'");

                if (shipInfo.MessageToShowOnPurchase != null)
                {
                    await player.SendAlertMessage(shipInfo.MessageToShowOnPurchase);
                }
            }
            else
            {
                // something changed since they wanted to buy it...
                _traceSource.TraceEvent(TraceEventType.Error, 0, $"Player '{player}' suddenly doesn't have enough money (Price:{shipInfo.Price}). Balance: {credits}");
                await player.SendAlarmMessage("Insufficient funds!");
            }
        }

        private async void ProcessBuyCommand(string restOfCommandString, Player player)
        {
            try
            {
                var playerPosition = await player.GetCurrentPosition(); // update current position

                var shipSeller = GetShipSellerAtPlayerLocation(playerPosition);

                if (shipSeller != null)
                {
                    if ((shipSeller.Origin == k_noOrigin) || (shipSeller.Origin == player.Origin))
                    {
                        if (string.IsNullOrWhiteSpace(restOfCommandString))
                        {
                            // print welcome and usage
                            await ShowWelcome(player, shipSeller);
                        }
                        else if (int.TryParse(restOfCommandString, out int originalNumber))
                        {
                            int number = originalNumber - 1;

                            if ((number >= 0) && (number < shipSeller.ShipsForSale.Count))
                            {
                                var shipInfo = shipSeller.ShipsForSale[number];

                                // look up credit balance
                                var credits = await player.GetCreditBalance();

                                if (credits >= shipInfo.Price)
                                {
                                    _traceSource.TraceInformation("Starting pending transaction for player '{0}' wanting to buy ship '{1}'", player, shipInfo.DisplayName);

                                    // ask for confirmation
                                    int answer = await player.ShowDialog($"Are you sure you want to buy \"{shipInfo.DisplayName}\"?", "Yes", "No");

                                    switch(answer)
                                    {
                                        case 0: // yes
                                            _traceSource.TraceInformation("Player '{0}' confirmed transaction to buy ship '{1}'", player, shipInfo.DisplayName);
                                            CompleteTransaction(player, shipInfo);
                                            break;
                                        case 1: // no
                                        default:
                                            _traceSource.TraceInformation("Player '{0}' canceled transaction to buy ship '{1}'", player, shipInfo.DisplayName);
                                            break;
                                    }
                                }
                                else
                                {
                                    // Need more $$$
                                    _traceSource.TraceEvent(TraceEventType.Error, 0, $"Player '{player}' doesn't have enough money (Price:{shipInfo.Price}). Balance: {credits}");
                                    await player.SendAlarmMessage("Insufficient funds!");
                                }
                            }
                            else
                            {
                                _traceSource.TraceEvent(TraceEventType.Error, 0, $"Player '{player}' invalid ship number to buy. ({originalNumber})");
                                await player.SendAlarmMessage("Invalid ship number to buy.");
                            }
                        }
                        else
                        {
                            // not an integer.  Print usage.
                            _traceSource.TraceEvent(TraceEventType.Error, 0, $"Player '{player}' invalid command to buy. ({restOfCommandString})");
                            await ShowUsage(player, shipSeller);
                        }
                    }
                    else
                    {
                        _traceSource.TraceEvent(TraceEventType.Error, 0, $"Player '{player}' not the right origin to buy. (player:{player.Origin}, seller:{shipSeller.Origin})");
                        await player.SendAlarmMessage("Not the right origin to buy a ship from.");
                    }
                }
                else
                {
                    _traceSource.TraceEvent(TraceEventType.Error, 0, $"Player '{player}' not in the right location to buy. ({playerPosition})");
                    await player.SendAlarmMessage("Not a valid place to buy a ship.");
                }
            }
            catch (Exception ex)
            {
                _traceSource.TraceEvent(TraceEventType.Error, 1, "ProcessBuyCommand Exception: {0}", ex.Message);
            }
        }

        private async Task ShowWelcome(Player player, Configuration.ShipSeller shipSeller)
        {
            var welcomeMessage = new StringBuilder($"Hi, I am {shipSeller.Name}.\nMy ships:\n");

            int index = 1;

            foreach(var shipInfo in shipSeller.ShipsForSale)
            {
                welcomeMessage.Append($"{index}. {shipInfo.DisplayName}: ${shipInfo.Price}\n");
                ++index;
            }

            await player.SendChatMessage(welcomeMessage.ToString());

            _traceSource.TraceInformation("Showed welcome for {0} to player {1}.", shipSeller.Name, player);
        }

        private async Task ShowUsage(Player player, Configuration.ShipSeller shipSeller)
        {
            await player.SendChatMessage($"Usage: \"{_config.BuyShipCommand} (number between 1 and {shipSeller.ShipsForSale.Count})\"");
        }

        private Configuration.ShipSeller GetShipSellerAtPlayerLocation(WorldPosition playerPosition)
        {
            foreach (var shipSeller in _config.ShipSellers)
            {
                BoundingBox boundingBox = new BoundingBox(_gameServerConnection, shipSeller.BoundingBox);

                if (boundingBox.IsInside(playerPosition))
                {
                    return shipSeller;
                }
            }

            return null;
        }
    }
}
