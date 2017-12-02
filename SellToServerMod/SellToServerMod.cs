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

                    // This returns a task that, when completed, has the selection from the player.
                    var task = player.DoItemExchange("Sell Items - Step 1", "Place Items to get a price", "Process"); // BUG: button text can only be set once "Get Price");

                    // This continues the operation later when the server returns the response.
                    task.ContinueWith(
                        async (Task<Eleon.Modding.ItemExchangeInfo> itemExchangeInfoInTask) =>
                        {
                            var itemExchangeInfoInQuote = itemExchangeInfoInTask.Result;

                            while (itemExchangeInfoInQuote.items != null)
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

                                credits = System.Math.Round(credits, 2);

                                var message = string.Format("We will pay you {0} credits.", credits);

                                // Here, instead of using ContinueWith, we just wait here for the response. Execution will resume as soon as we get back an answer.
                                var itemExchangeInfoSold = await player.DoItemExchange(
                                    "Sell Items - Step 2",
                                    message,
                                    "Process", // BUG: button text can only be set once "Sell Items",
                                    itemExchangeInfoInQuote.items);

                                if ((itemExchangeInfoSold.items != null) && (itemExchangeInfoSold.items.AreTheSame(itemExchangeInfoInQuote.items)))
                                {
                                    _gameServerConnection.DebugOutput("Player {0} sold items for {1} credits.", player, credits);
                                    await player.AddCredits(credits);
                                    await player.SendAlertMessage("Items sold for {0} credits.", credits);
                                    break;
                                }
                                else
                                {
                                    // try again if the user changed things.
                                    _gameServerConnection.DebugOutput("Player {0} changed things.", player);
                                    itemExchangeInfoInQuote = itemExchangeInfoSold;
                                }
                            }
                        });

                    break;
                }
            }

            if (!found)
            {
                _gameServerConnection.DebugOutput("player not in the right spot: {0}", player.Position);
                player.SendAlarmMessage("Not a valid place to sell.");
            }
        }

        private IGameServerConnection _gameServerConnection;
        private Configuration _config;
    }
}
