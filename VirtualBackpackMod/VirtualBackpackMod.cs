using EmpyrionModApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eleon.Modding;

namespace VirtualBackpackMod
{
    // This attribute lets the mod runner find it later.
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class VirtualBackpackMod : IGameMod
    {
        // This is the string that will be listed when a user types "!MODS".
        // The helper method here uses the AssemblyTitle attribute found in the AssemblyInfo.cs.
        static readonly string k_versionString = EmpyrionModApi.Helpers.GetVersionString(typeof(VirtualBackpackMod));


        // This is called by the mod runner before connecting to the game server during startup.
        public void Start(IGameServerConnection gameServerConnection)
        {
            _fileStoragePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\SavedData";

            // save connection to game server for later use
            _gameServerConnection = gameServerConnection;

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
        private async void OnEvent_ChatMessage(ChatType chatType, string msg, Player player)
        {
            try
            {
                switch (msg)
                {
                    case "/backpack":
                        await ProcessBackpackCommand(player);
                        break;
                }
            }
            catch(Exception ex)
            {
                _gameServerConnection.DebugOutput("VirtualBackpackMod Exception: {0}", ex.Message);
            }
        }

        private async Task ProcessBackpackCommand(Player player)
        {
            var backpackItems = await GetBackpackForPlayer(player);

            // await continues the operation later when the server returns the response.
            var updatedBackpackItems = await player.DoItemExchange(
                "Virtual Backpack",
                "Extra Inventory Space, Yay!",
                "Save",
                backpackItems);

            await SaveBackpackForPlayer(player, updatedBackpackItems.items);
        }

        private async Task<Eleon.Modding.ItemStack[]> GetBackpackForPlayer(Player player)
        {
            var resultStackList = new List<Eleon.Modding.ItemStack>();
            if (System.IO.File.Exists(GetPlayerBackpackFilePath(player)))
            {
                using (var reader = System.IO.File.OpenText(GetPlayerBackpackFilePath(player)))
                {
                    string bagLine;
                    while ((bagLine = await reader.ReadLineAsync()) != null)
                    {
                        string[] bagLinesSplit = bagLine.Split(',');
                        var itStack = new Eleon.Modding.ItemStack(Convert.ToInt32(bagLinesSplit[1]), Convert.ToInt32(bagLinesSplit[2])); //1=ItemNumber, 2=StackSize
                        itStack.slotIdx = Convert.ToByte(bagLinesSplit[0]);//0=SlotNumber
                        itStack.ammo = Convert.ToInt32(bagLinesSplit[3]);//3=Ammo
                        itStack.decay = Convert.ToInt32(bagLinesSplit[4]);//4=Decay
                        resultStackList.Add(itStack);
                    }
                }
            }

            return resultStackList.ToArray();
        }

        private async Task SaveBackpackForPlayer(Player player, Eleon.Modding.ItemStack[] updatedBackpackItems)
        {
            using (var writer = System.IO.File.CreateText(GetPlayerBackpackFilePath(player)))
            {
                foreach (var itemStack in updatedBackpackItems)
                {
                    await writer.WriteLineAsync($"{itemStack.slotIdx},{itemStack.id},{itemStack.count},{itemStack.ammo},{itemStack.decay}");
                }
            }
        }

        private string GetPlayerBackpackFilePath(Player player)
        {
            var playerDirectory = System.IO.Path.Combine(_fileStoragePath, $"players\\EID{player.EntityId}");
            System.IO.Directory.CreateDirectory(playerDirectory);

            return System.IO.Path.Combine(playerDirectory, $"VirtualBackpack.txt");
        }

        private string _fileStoragePath;
        private IGameServerConnection _gameServerConnection;
    }
}
