using System.Collections.Generic;

namespace DiscordBotMod
{
    public class Configuration
    {
        public string DiscordToken { get; set; }

        public ulong ChannelId { get; set; }

        public string FromGameFormattingString { get; set; }

        public string FromDiscordFormattingString { get; set; }
    }
}
