using SharedCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkConnectedModRunner
{
    class Program
    {
        static GameServerConnection _gameServerConnection;

        static Configuration config;

        static void Main(string[] args)
        {
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "Settings.yaml";

            //Configuration.TestFormat(configFilePath + ".yaml");

            config = Configuration.GetConfiguration<Configuration>(configFilePath);

            using (_gameServerConnection = new GameServerConnection(config))
            {
                var sellToServerMod = new SellToServerMod.SellToServerMod(_gameServerConnection, config.SellToServerMod);
                var factionPlayfieldKickerMod = new FactionPlayfieldKickerMod.FactionPlayfieldKickerMod(_gameServerConnection, config.FactionPlayfieldKickerMod);

                _gameServerConnection.Connect();

                // wait until the user presses Enter.
                string input = Console.ReadLine();
            }


        }
    }
}
