using DSharpPlus;
using SharedCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBotMod
{
    // This attribute lets the mod runner find it later.
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class DiscordBotMod : IGameMod
    {
        // This is the string that will be listed when a user types "!MODS".
        // The helper method here uses the AssemblyTitle attribute found in the AssemblyInfo.cs.
        static readonly string k_versionString = SharedCode.Helpers.GetVersionString(typeof(DiscordBotMod));


        // This is called by the mod runner before connecting to the game server during startup.
        public void Start(IGameServerConnection gameServerConnection)
        {
            // figure out the path to the setting file in the same folder where this DLL is located.
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "DiscordBotMod_Settings.yaml";

            // save connection to game server for later use
            _gameServerConnection = gameServerConnection;

            // This deserializes the yaml config file
            _config = SharedCode.BaseConfiguration.GetConfiguration<Configuration>(configFilePath);

            // Tell the string to use for "!MODS" command.
            _gameServerConnection.AddVersionString(k_versionString);

            _discordClient = new DiscordClient(new DiscordConfiguration
            {
                Token = _config.DiscordToken,
                TokenType = TokenType.Bot
            });

            // Subscribe for the chat event
            _gameServerConnection.Event_ChatMessage += OnEvent_ChatMessage;

            _discordClient.MessageCreated += async e =>
            {
                DSharpPlus.Entities.DiscordChannel discordChannel;
                lock (_discordClient)
                {
                    discordChannel = _discordChannel;
                }

                if (e.Author != _discordClient.CurrentUser)
                {
                    if(e.Message.Content == "cb:get_online_users")
                    {
                        // build list of online players:
                        string onlinePlayers = string.Join("\n", from player in _gameServerConnection.GetOnlinePlayers().Values select player.Name);

                        var dmUserChannel = await _discordClient.CreateDmAsync(e.Author);

                        await _discordClient.SendMessageAsync(dmUserChannel, $"Online Players:\n{onlinePlayers}");
                    }
                    else if (e.Channel == discordChannel)
                    {
                        await _gameServerConnection.SendChatMessageToAll(string.Format(_config.FromDiscordFormattingString, e.Author.Username, e.Message.Content));
                    }
                }
            };

            _discordClient.ConnectAsync()
                .ContinueWith(async t =>
                {
                    var channel = await _discordClient.GetChannelAsync(_config.ChannelId);
                    lock (_discordClient)
                    {
                        _discordChannel = channel;
                    }
                });
        }

        // This is called right before the program ends.  Mods should save anything they need here.
        public void Stop()
        {
            _discordClient.DisconnectAsync().Wait(2 * 1000);
            _discordClient.Dispose();
        }


        // Event handler for when chat message are received from players.
        private void OnEvent_ChatMessage(ChatType chatType, string msg, Player player)
        {
            if (chatType == ChatType.ToAll)
            {
                lock (_discordClient)
                {
                    if (_discordChannel != null)
                    {
                        _discordClient.SendMessageAsync(_discordChannel, string.Format(_config.FromGameFormattingString, player.Name, msg));
                    }
                }
            }
        }

        private IGameServerConnection _gameServerConnection;
        private Configuration _config;
        private DiscordClient _discordClient;
        private DSharpPlus.Entities.DiscordChannel _discordChannel;
    }
}
