using SharedCode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VotingRewardMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class VotingRewardMod : IGameMod
    {
        static readonly string k_versionString = SharedCode.Helpers.GetVersionString(typeof(VotingRewardMod));

        public void Start(IGameServerConnection gameServerConnection)
        {
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "VotingRewardMod_Settings.yaml";

            _gameServerConnection = gameServerConnection;
            _config = SharedCode.BaseConfiguration.GetConfiguration<Configuration>(configFilePath);

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_ChatMessage += OnEvent_ChatMessage;
        }

        public void Stop()
        {
        }

        private async void OnEvent_ChatMessage(ChatType chatType, string msg, Player player)
        {
            switch (msg)
            {
                case "/votereward":
                    {
                        if (await DoesPlayerHaveReward(player))
                        {
                            var itemExchangeInfo = await player.DoItemExchange("Voting Reward", "Remember to vote everyday. Enjoy!", "Close", _config.VotingRewards.ToEleonArray());

                            await MarkRewardClaimed(player);
                        }
                        else
                        {
                            await player.SendAlarmMessage("No unclaimed voting reward found.");
                        }
                    }
                    break;
            }
        }

        private async Task<bool> DoesPlayerHaveReward(Player player)
        {
            var uri = $"https://empyrion-servers.com/api/?object=votes&element=claim&key={_config.VotingApiServerKey}&steamid={player.SteamId}";

            var response = await CallRestMethod("GET", uri);

            return (response == "1");
        }

        private async Task MarkRewardClaimed(Player player)
        {
            var uri = $"https://empyrion-servers.com/api/?action=post&object=votes&element=claim&key={_config.VotingApiServerKey}&steamid={player.SteamId}";

            await CallRestMethod("POST", uri);
        }

        public async static Task<string> CallRestMethod(string method, string url)
        {
            var webrequest = WebRequest.Create(url);
            webrequest.Method = method;
            using (var response = (HttpWebResponse)await Task.Factory.FromAsync<WebResponse>(webrequest.BeginGetResponse, webrequest.EndGetResponse, null))
            {
                var responseStream = new StreamReader(response.GetResponseStream(), System.Text.Encoding.GetEncoding("utf-8"));
                string result = string.Empty;
                result = responseStream.ReadToEnd();

                return result;
            }
        }

        private IGameServerConnection _gameServerConnection;
        private Configuration _config;
    }
}
