using SharedCode;
using SharedCode.ExtensionMethods;
using System.Threading.Tasks;

namespace SellToServerMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class SellToServerMod : IGameMod
    {
        static readonly string k_versionString = SharedCode.Helpers.GetVersionString(typeof(SellToServerMod));

        public void Start(IGameServerConnection gameServerConnection)
        {
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "SellToServerMod_Settings.yaml";

            _gameServerConnection = gameServerConnection;
            _config = SharedCode.BaseConfiguration.GetConfiguration<Configuration>(configFilePath);

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_ChatMessage += OnEvent_ChatMessage;
        }

        public void Stop()
        {
        }

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
                    var task = player.DoItemExchange("Sell Items - Step 1", "Place Items to get a price", "Process"); // BUG: button text can only be set once "Get Price");

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
                                    // try again
                                    _gameServerConnection.DebugOutput("Player {0} changed things.", player);
                                    itemExchangeInfoInQuote = itemExchangeInfoSold;
                                }
                            }
                        });
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
