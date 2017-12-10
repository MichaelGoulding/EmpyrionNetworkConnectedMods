using EmpyrionModApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminPowerToolsMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class AdminPowerToolsMod : IGameMod
    {
        static readonly string k_versionString = EmpyrionModApi.Helpers.GetVersionString(typeof(AdminPowerToolsMod));


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
                if (msg.StartsWith("/inbox"))
                {
                    await ProcessInboxCommand(player);
                }
                else if (player.IsPrivileged)
                {
                    if (msg.StartsWith("/mail"))
                    {
                        await ProcessMailCommand(msg, player);
                    }
                    else if (msg.StartsWith("/find"))
                    {
                        await ProcessFindCommand(msg, player);
                    }
                }

                // /find
                // /inbox
                // /mail
                // "Usage: /send Xango2000 Message that player receives\r\nOR /send Xan Anything you want to say\r\nOR /send 12345\r\nSubject not required. Can use part of the player name or their empyrion ID"
            }
            catch (Exception ex)
            {
                _gameServerConnection.DebugOutput("VirtualBackpackMod Exception: {0}", ex.Message);
            }
        }

        private async Task ProcessInboxCommand(Player player)
        {
            var mailBoxData = await GetMailboxForPlayer(player);

            // await continues the operation later when the server returns the response.
            var updatedMailBoxItems = await player.DoItemExchange(
                "Virtual Backpack",
                "Extra Inventory Space, Yay!",
                "Save",
                mailBoxData.Items);

            mailBoxData.Items = updatedMailBoxItems.items;

            await SaveMailboxForPlayer(player, mailBoxData);
        }

        private async Task ProcessMailCommand(string msg, Player player)
        {
            //WIP
            await player.ShowDialog("Not Implemented!", MessagePriority.Alarm, 10);
        }

        private async Task ProcessFindCommand(string msg, Player player)
        {
            var targets = PlayersFromNameFragment(msg);

            if (targets.Count == 0) //Error
            {  
                //WIP
                await player.ShowDialog("No Players Found", MessagePriority.Alarm, 10);
            }
            else //Actual player's Empyrion ID
            {
                await Task.WhenAll(
                    from targetedPlayer in targets
                    select player.SendChatMessage($"[{targetedPlayer.MemberOfFaction.Initials}]{targetedPlayer.Name} @{targetedPlayer.Position} #{targetedPlayer.EntityId}"));
            }
        }

        private List<Player> PlayersFromNameFragment(string chatMessage)
        {
            var exactMatches = new List<Player> { };
            var nearMatches = new List<Player> { };
            if (chatMessage.Contains(" "))
            {
                var messagePieces = chatMessage.Split(' ');
                string message1 = messagePieces[1];
                foreach (var player in _gameServerConnection.GetOnlinePlayers().Values)
                {
                    if( (player.EntityId.ToString() == message1) || (player.Name == message1))
                    {
                        exactMatches.Add(player);
                    }
                    else if((player.Name.Contains(message1)) || (player.Name.ToLower().Contains(message1.ToLower())))
                    {
                        nearMatches.Add(player);
                    }
                }

                if (exactMatches.Count != 1)
                {
                    exactMatches.AddRange(nearMatches);
                }
            }

            return exactMatches;
        }

        class MailBoxData
        {
            public string Message { get; set; }
            public Eleon.Modding.ItemStack[] Items { get; set; }
        }

        private async Task<MailBoxData> GetMailboxForPlayer(Player player)
        {
            var result = new MailBoxData();
            var resultStackList = new List<Eleon.Modding.ItemStack>();

            if (System.IO.File.Exists(GetPlayerMailboxFilePath(player)))
            {
                using (var reader = System.IO.File.OpenText(GetPlayerMailboxFilePath(player)))
                {
                    string mailBoxLine;
                    while ((mailBoxLine = await reader.ReadLineAsync()) != null)
                    {
                        if (result.Message == null)
                        {
                            //var Message = UserMail[0].Split(new[] { ',' }, 4); //split first line of user mail: Timestamp, Sender, New?, Message
                            //ItemStack[] MailContents = buildItemStack("Content\\Mods\\Xango\\Mail\\" + Message[0] + ".txt");
                            result.Message = mailBoxLine;
                        }
                        else
                        {
                            string[] bagLinesSplit = mailBoxLine.Split(',');
                            var itStack = new Eleon.Modding.ItemStack(Convert.ToInt32(bagLinesSplit[1]), Convert.ToInt32(bagLinesSplit[2])); //1=ItemNumber, 2=StackSize
                            itStack.slotIdx = Convert.ToByte(bagLinesSplit[0]);//0=SlotNumber
                            itStack.ammo = Convert.ToInt32(bagLinesSplit[3]);//3=Ammo
                            itStack.decay = Convert.ToInt32(bagLinesSplit[4]);//4=Decay
                            resultStackList.Add(itStack);
                        }
                    }
                }
            }

            result.Items = resultStackList.ToArray();

            return result;
        }

        private async Task SaveMailboxForPlayer(Player player, MailBoxData mailBoxData)
        {
            using (var writer = System.IO.File.CreateText(GetPlayerMailboxFilePath(player)))
            {
                await writer.WriteLineAsync(mailBoxData.Message);
                foreach (var itemStack in mailBoxData.Items)
                {
                    await writer.WriteLineAsync($"{itemStack.slotIdx},{itemStack.id},{itemStack.count},{itemStack.ammo},{itemStack.decay}");
                }
            }
        }

        private string GetPlayerMailboxFilePath(Player player)
        {
            var playerDirectory = System.IO.Path.Combine(_fileStoragePath, $"players\\EID{player.EntityId}");
            System.IO.Directory.CreateDirectory(playerDirectory);

            return System.IO.Path.Combine(playerDirectory, $"mail.txt");
        }

        private string _fileStoragePath;
        private IGameServerConnection _gameServerConnection;
    }
}
