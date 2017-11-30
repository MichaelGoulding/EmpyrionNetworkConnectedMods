using SharedCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace StructureOwnershipMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class StructureOwnershipMod : IGameMod
    {
        static readonly string k_versionString = SharedCode.Helpers.GetVersionString(typeof(StructureOwnershipMod));

        public void Start(GameServerConnection gameServerConnection)
        {
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "StructureOwnershipMod_Settings.yaml";

            _gameServerConnection = gameServerConnection;
            _config = SharedCode.BaseConfiguration.GetConfiguration<Configuration>(configFilePath);

            var saveStateFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "SaveState.yaml";
            _factionRewardTimer = new Timer(_config.CaptureRewardMinutes * 1000.0 * 60.0);

            _saveState = SaveState.Load(saveStateFilePath);

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_Faction_Changed += OnEvent_Faction_Changed;
            _gameServerConnection.Event_ChatMessage += OnEvent_ChatMessage;

            _factionRewardTimer.Elapsed += OnFactionRewardTimer_Elapsed;
            if (!_factionRewardTimer.Enabled)
            {
                _factionRewardTimer.Start();
            }
        }

        public void Stop()
        {
        }

        private void OnEvent_ChatMessage(string msg, Player player)
        {
            if (msg == "/income")
            {
                var items = new Eleon.Modding.ItemStack[38];

                for (int i = 0; i < items.Length; ++i)
                {
                    items[i] = new Eleon.Modding.ItemStack(2273, i + 1234567890);
                }

                var task = player.DoItemExchange("test1", "test2", "Process", items);

                task.ContinueWith(
                    (Task<Eleon.Modding.ItemExchangeInfo> itemExchangeInfoInTask) =>
                    {
                        var itemExchangeInfoInQuote = itemExchangeInfoInTask.Result;

                        ;

                    });
            }
        }

        private void OnEvent_Faction_Changed(Eleon.Modding.FactionChangeInfo obj)
        {
            lock (_saveState)
            {
                foreach (var o in _config.EntityCaptureItems)
                {
                    if (obj.id == o.EntityId)
                    {
                        if(!_saveState.FactionIdToEntityIds.ContainsKey(obj.factionId))
                        {
                            _saveState.FactionIdToEntityIds.Add(obj.factionId, new HashSet<int>());
                        }

                        _saveState.FactionIdToEntityIds[obj.factionId].Add(obj.id);

                        if (_saveState.EntityIdToFactionId.ContainsKey(obj.id))
                        {
                            // remove entity id from old faction
                            _saveState.FactionIdToEntityIds[_saveState.EntityIdToFactionId[obj.id]].Remove(obj.id);
                        }

                        _saveState.EntityIdToFactionId[obj.id] = obj.factionId;
                        break;
                    }
                }
            }
        }

        private void OnFactionRewardTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var onlinePlayersById = _gameServerConnection.GetOnlinePlayers();
            lock (onlinePlayersById)
            {
                lock (_saveState)
                {
                    foreach (var playerId in onlinePlayersById.Keys)
                    {
                        if (_saveState.FactionIdToEntityIds.ContainsKey(onlinePlayersById[playerId].MemberOfFaction.Id))// _players[entityId].MemberOfFaction.Id == saveState.OwningFactionId)
                        {
                            var factoryContents = onlinePlayersById[playerId].BpResourcesInFactory;
                            var itemStacks = new List<Eleon.Modding.ItemStack>();

                            foreach (var entityId in _saveState.FactionIdToEntityIds[onlinePlayersById[playerId].MemberOfFaction.Id])
                            {
                                //var oldCobaltAmount = factoryContents.ContainsKey(ItemId.Ingot_Cobalt) ? (int)factoryContents[ItemId.Ingot_Cobalt] : 0;
                                //var oldSiliconAmount = factoryContents.ContainsKey(ItemId.Ingot_Silicon) ? (int)factoryContents[ItemId.Ingot_Silicon] : 0;
                                //itemStacks.Add(new ItemStack(2273, oldCobaltAmount + 2000));
                                //itemStacks.Add(new ItemStack(2274, oldSiliconAmount + 2000));
                                itemStacks.Add(new Eleon.Modding.ItemStack(2273, 2000));
                                itemStacks.Add(new Eleon.Modding.ItemStack(2274, 2000));
                            }

                            _gameServerConnection.SendRequest(
                                Eleon.Modding.CmdId.Request_Blueprint_Resources,
                                new Eleon.Modding.BlueprintResources(playerId, itemStacks, false));
                        }
                    }
                }
            }
        }

        private GameServerConnection _gameServerConnection;
        private Configuration _config;
        private SaveState _saveState;
        private Timer _factionRewardTimer;

    }
}
