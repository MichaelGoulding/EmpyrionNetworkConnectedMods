using SharedCode;
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

        static GameServerConnection _gameServerConnection;

        static Configuration config;

        static void Main(string[] args)
        {
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "Settings.yaml";

            //Configuration.TestFormat(configFilePath + ".yaml");

            config = Configuration.GetConfiguration<Configuration>(configFilePath);

            using (_gameServerConnection = new GameServerConnection(config))
            {
                var sellToServerMod = new SellToServerMod.SellToServerMod(_gameServerConnection, config.SellToServerModConfiguration);

                _gameServerConnection.Connect();

                // wait until the user presses Enter.
                string input = Console.ReadLine();
            }


        }

        private static void OnEvent_ChatMessage(Eleon.Modding.ChatInfo chatInfo, Player player)
        {
            switch (chatInfo.msg)
            {
                case "/trade":
                    ProcessTradeCommand(player);
                    break;
            }
        }


        private static void ProcessTradeCommand(Player player)
        {
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
    }
}
