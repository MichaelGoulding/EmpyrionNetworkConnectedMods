using SharedCode.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SecureTrading
{
    //class TradeTransaction
    //{
    //    int player1;
    //    int player2;

    //    int itemsBeingOfferedByPlayer1;
    //    int itemsBeingOfferedByPlayer2;

    //    bool player1Accepted;
    //    bool player2Accepted;

    //    async Task<int> GetItemsFromPlayer(int playerId)
    //    {
    //        return 0; // TODO
    //    }

    //    void TellEachPlayer(string format, params object[] args)
    //    {
    //    }


    //    async void StartTrading()
    //    {
    //        itemsBeingOfferedByPlayer1 = await GetItemsFromPlayer(player1);
    //        //TellEachPlayer("{0} offers: {1}", );
    //        itemsBeingOfferedByPlayer2 = await GetItemsFromPlayer(player2);

    //    }
    //}

    class Program
    {
        static readonly string k_versionString = (typeof(Program).GetTypeInfo().Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute)).SingleOrDefault() as AssemblyTitleAttribute).Title;

        static SharedCode.GameServerConnection _gameServerConnection;

        static Configuration config;

        static void Main(string[] args)
        {
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "Settings.yaml";

            config = Configuration.GetConfiguration<Configuration>(configFilePath);

            using (_gameServerConnection = new SharedCode.GameServerConnection(k_versionString, config))
            {
                _gameServerConnection.Event_ChatMessage += OnEvent_ChatMessage;

                _gameServerConnection.Connect();

                // wait until the user presses Enter.
                string input = Console.ReadLine();
            }

            // on trade command

            // check area for nearest players,  if more than 1 is closer than 50 meters, ask if closest is acceptable
            // pop up item request
            // accept items
            // show both players what's being traded via chat.
            // accept items from other player
            // show both players what's being traded via chat.
            // ask both players if they are happy
            // if both confirm
            // show opposite items back to players
            // if canceled, give same items back

        }

        private static void OnEvent_ChatMessage(Eleon.Modding.ChatInfo chatInfo, SharedCode.Player player)
        {
            if( chatInfo.msg == "/sell")
            {
                var task = _gameServerConnection.DoItemExchangeWithPlayer(player, "Sell Items - Step 1", "Place Items to get a price", "Get Price");

                task.ContinueWith(
                    async (Task<Eleon.Modding.ItemExchangeInfo> itemExchangeInfoInTask) =>
                    {
                        var itemExchangeInfoInQuote = itemExchangeInfoInTask.Result;

                        while (itemExchangeInfoInQuote.items != null)
                        {
                            double credits = 0;
                            foreach (var stack in itemExchangeInfoInQuote.items)
                            {
                                if(config.ItemIdToUnitPrice.TryGetValue( stack.id, out double value))
                                {
                                    credits += value * stack.count;
                                }
                            }

                            var message = string.Format("We will pay you {0} credits.", credits);

                            var itemExchangeInfoSold = await _gameServerConnection.DoItemExchangeWithPlayer(player, "Sell Items - Step 2", message, "Sell Items", itemExchangeInfoInQuote.items);

                            if((itemExchangeInfoSold.items != null) && (itemExchangeInfoSold.items.AreTheSame(itemExchangeInfoInQuote.items)))
                            {
                                _gameServerConnection.DebugOutput("Player {0} sold items for {1} credits.", player, credits);
                                player.AddCredits(credits);
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
    }
}
