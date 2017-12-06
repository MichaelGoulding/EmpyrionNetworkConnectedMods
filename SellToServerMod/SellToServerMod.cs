using SharedCode;
using SharedCode.ExtensionMethods;
using System.Threading.Tasks;

namespace SellToServerMod
{
    // This attribute lets the mod runner find it later.
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class SellToServerMod : IGameMod
    {
        // This is the string that will be listed when a user types "!MODS".
        // The helper method here uses the AssemblyTitle attribute found in the AssemblyInfo.cs.
        static readonly string k_versionString = SharedCode.Helpers.GetVersionString(typeof(SellToServerMod));


        // This is called by the mod runner before connecting to the game server during startup.
        public void Start(IGameServerConnection gameServerConnection)
        {
            // figure out the path to the setting file in the same folder where this DLL is located.
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "SellToServerMod_Settings.yaml";

            // save connection to game server for later use
            _gameServerConnection = gameServerConnection;

            // This deserializes the yaml config file
            _config = SharedCode.BaseConfiguration.GetConfiguration<Configuration>(configFilePath);

            // Tell the string to use for "!MODS" command.
            _gameServerConnection.AddVersionString(k_versionString);

            // Subscribe for the chat event
            _gameServerConnection.Event_ChatMessage += OnEvent_ChatMessage;
        }


        // This is called right before the program ends.  Mods should save anything they need here.
        public void Stop()
        {
        }


        // Event handler for when chat message are received from players.
        private void OnEvent_ChatMessage(string msg, Player player)
        {
            switch (msg)
            {
                case "/sell":
                    ProcessSellCommand(player);
                    break;
            }
        }

        private void ProcessSellCommand(Player player)
        {
            bool found = false;
            foreach (var sellLocation in _config.SellLocations)
            {
                BoundingBox boundingBox = new BoundingBox(_gameServerConnection, sellLocation.BoundingBox);

                if (boundingBox.IsInside(player))
                {
                    found = true;

                    DoSellTransaction(player, sellLocation);

                    break;
                }
            }

            if (!found)
            {
                _gameServerConnection.DebugOutput("player not in the right spot: {0}", player.Position);
                player.SendAlarmMessage("Not a valid place to sell.");
            }
        }

        private async void DoSellTransaction(Player player, Configuration.SellLocation sellLocation)
        {
            // await continues the operation later when the server returns the response.
            var itemExchangeInfoInQuote = await player.DoItemExchange(
                title: "Sell Items - Step 1",
                description: "Place Items to get a price",
                buttonText: "Process"); // BUG: button text can only be set once, otherwise I would put "Get Price"

            // If the player ever removes all items from the item exchange window, stop the transaction.
            while (itemExchangeInfoInQuote.items != null)
            {
                // calculate the worth of the items the player put in the item exchange window
                double credits = GetSellValueOfItems(sellLocation, itemExchangeInfoInQuote);

                // Show the price with the same items he put in, in case he wants to adjust his order.
                var itemExchangeInfoSold = await player.DoItemExchange(
                    "Sell Items - Step 2",
                    $"We will pay you {credits} credits.",
                    "Process", // BUG: button text can only be set once "Sell Items",
                    itemExchangeInfoInQuote.items);

                if ((itemExchangeInfoSold.items != null) && (itemExchangeInfoSold.items.AreTheSame(itemExchangeInfoInQuote.items)))
                {
                    // the player didn't change his items, complete the purchase
                    _gameServerConnection.DebugOutput("Player {0} sold items for {1} credits.", player, credits);
                    await player.AddCredits(credits);
                    await player.SendAlertMessage("Items sold for {0} credits.", credits);
                    break;
                }
                else
                {
                    // if the items changed, continue the while loop with the new items returned.
                    _gameServerConnection.DebugOutput("Player {0} changed things.", player);
                    itemExchangeInfoInQuote = itemExchangeInfoSold;
                }
            }
        }

        private static double GetSellValueOfItems(Configuration.SellLocation sellLocation, Eleon.Modding.ItemExchangeInfo itemExchangeInfoInQuote)
        {
            double credits = 0;

            foreach (var stack in itemExchangeInfoInQuote.items)
            {
                double unitPrice;
                if (!sellLocation.ItemIdToUnitPrice.TryGetValue(stack.id, out unitPrice))
                {
                    unitPrice = sellLocation.DefaultPrice;
                }

                credits += unitPrice * stack.count;
            }

            return System.Math.Round(credits, 2);
        }

        private IGameServerConnection _gameServerConnection;
        private Configuration _config;
    }
}
