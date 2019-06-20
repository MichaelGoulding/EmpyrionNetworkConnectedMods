using EmpyrionModApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactionStorageMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class FactionStorageMod : IGameMod
    {
        static readonly string k_versionString = EmpyrionModApi.Helpers.GetVersionString(typeof(FactionStorageMod));

        static readonly string k_saveStateFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "FactionStorageMod_SaveState.yaml";

        private TraceSource _traceSource = new TraceSource("FactionStorageMod");
        private IGameServerConnection _gameServerConnection;
        private Configuration _config;
        private SaveState _saveState;
        private HashSet<int> _factionStorageScreensOpen = new HashSet<int>(); // key is the faction id
        

        public void Start(IGameServerConnection gameServerConnection)
        {
            _traceSource.TraceInformation("Starting up...");
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "FactionStorageMod_Settings.yaml";

            _gameServerConnection = gameServerConnection;
            _config = BaseConfiguration.GetConfiguration<Configuration>(configFilePath);

            _saveState = SaveState.Load(k_saveStateFilePath);

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_ChatMessage += OnEvent_ChatMessage;
        }

        public void Stop()
        {
            lock (_saveState)
            {
                _saveState.Save(k_saveStateFilePath);
            }

            _traceSource.TraceInformation("Stopping...");
            _traceSource.Flush();
            _traceSource.Close();
        }

        private async void OnEvent_ChatMessage(ChatType chatType, string msg, Player player)
        {
            if (msg == _config.FactionStorageCommand)
            {
                _traceSource.TraceInformation($"Player '{player}' asked to see their faction's shared storage.");

                await OnUseFactionStorage(player);
            }
        }

        private async Task OnUseFactionStorage(Player player)
        {
            await _gameServerConnection.RefreshFactionList();
            await player.RefreshInfo();

            lock (_saveState)
            {
                if ((player.MemberOfFaction != null))
                {
                    int factionId = player.MemberOfFaction.Id;

                    if (_factionStorageScreensOpen.Contains(factionId))
                    {
                        _traceSource.TraceInformation($"Player '{player}' can't use the shared storage because someone else is using it still.");
                        player.SendAlarmMessage("Another member of your faction has the storage window open.");
                    }
                    else
                    {
                        _factionStorageScreensOpen.Add(factionId);
                        _traceSource.TraceInformation($"Player '{player}' is now using the shared storage.");

                        if (!_saveState.FactionIdToItemStacks.ContainsKey(factionId))
                        {
                            _traceSource.TraceInformation($"Player '{player}' is now creating the shared storage as it didn't exist earlier.");
                            _saveState.FactionIdToItemStacks[factionId] = new ItemStacks();
                        }

                        var storage = _saveState.FactionIdToItemStacks[factionId];
                        var task = player.DoItemExchange(
                            "Shared faction storage",
                            $"Items shared with the {player.MemberOfFaction.Name} faction",
                            "Process",
                            storage.ExtractOutForItemExchange());

                        task.ContinueWith(
                            (Task<Eleon.Modding.ItemExchangeInfo> itemExchangeInfoInTask) =>
                            {
                                lock (_saveState)
                                {
                                    var itemExchangeInfoInQuote = itemExchangeInfoInTask.Result;
                                    storage.AddStacks(new ItemStacks(itemExchangeInfoInQuote.items));
                                    _factionStorageScreensOpen.Remove(factionId);
                                    _saveState.Save(k_saveStateFilePath);
                                    _traceSource.TraceInformation($"Player '{player}' is now done using the shared storage.");
                                }
                            });
                    }
                }
                else
                {
                    _traceSource.TraceInformation($"Player '{player}' is not in a faction");
                    player.SendAlarmMessage("You need to be in a faction to use shared faction storage.");
                }
            }
        }
    }
}
