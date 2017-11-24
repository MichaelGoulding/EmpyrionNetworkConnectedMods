using Eleon.Modding;
using EPMConnector;
using SharedCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace PlanetOwnership
{
    // TODO:  keep track of playfield ownership
    // TODO: create API to send messages to players, factions, etc.
    // TODO: have module that keeps track of loaded playfields and then queries for structures on that playfield if owned.
    // ask for structures for that playfield regularly, and if factionId != 0 && != owningFactionId, then prompt before destorying them in X minutes.


    class Program
    {
        static readonly string k_versionString = SharedCode.VersionHelper.GetVersionString(typeof(Program));

        static SharedCode.GameServerConnection _gameServerConnection;

        static Config.Configuration config;

        static Config.SaveState saveState;

        static Timer _factionRewardTimer;


        static void Main(string[] args)
        {
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "Settings.yaml";
            var saveStateFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "SaveState.yaml";

            config = Config.Configuration.GetConfiguration<Config.Configuration>(configFilePath);

            _factionRewardTimer = new Timer(config.CaptureRewardMinutes * 1000.0 * 60.0);

            saveState = Config.SaveState.Load(saveStateFilePath);

            using (_gameServerConnection = new SharedCode.GameServerConnection(config))
            {
                _gameServerConnection.AddVersionString(k_versionString);
                _gameServerConnection.Event_Faction_Changed += OnEvent_Faction_Changed;

                BoundingBox bbox = new BoundingBox("Akua2", new Rect3(new Vector3(0, 0, 0), new Vector3(100, 100, 100)));

                _gameServerConnection.AddBoundingBox(bbox);

                _gameServerConnection.Connect();

                if (!_factionRewardTimer.Enabled)
                {
                    _factionRewardTimer.Start();
                }

                //OwnershipChangeChecker ownershipChangeChecker = new OwnershipChangeChecker(new Faction(0), bbox);

                // get/refresh data
                _factionRewardTimer.Elapsed += OnFactionRewardTimer_Elapsed;

                // wait until the user presses Enter.
                string input = Console.ReadLine();

                _factionRewardTimer.Stop();

                saveState.Save(saveStateFilePath);
            }
        }

        private static void OnEvent_Faction_Changed(FactionChangeInfo obj)
        {
            if (obj.id == config.EntityToCapture)
            {
                // TODO:
                //saveState.FactionIdToEntityIds[obj.id] = obj.factionId;
            }
        }

        private static void OnFactionRewardTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //lock (_players)
            //{
            //    foreach (var playerId in _players.Keys)
            //    {
            //        if (saveState.FactionIdToEntityIds.ContainsKey(_players[playerId].MemberOfFaction.Id) )// _players[entityId].MemberOfFaction.Id == saveState.OwningFactionId)
            //        {
            //            foreach(var entityId in saveState.FactionIdToEntityIds[_players[playerId].MemberOfFaction.Id])
            //            {

            //            }
            //            var factoryContents = _players[playerId].BpResourcesInFactory;

            //            List<ItemStack> itemStacks = new List<ItemStack>();
            //            //var oldCobaltAmount = factoryContents.ContainsKey(ItemId.Ingot_Cobalt) ? (int)factoryContents[ItemId.Ingot_Cobalt] : 0;
            //            //var oldSiliconAmount = factoryContents.ContainsKey(ItemId.Ingot_Silicon) ? (int)factoryContents[ItemId.Ingot_Silicon] : 0;
            //            //itemStacks.Add(new ItemStack(2273, oldCobaltAmount + 2000));
            //            //itemStacks.Add(new ItemStack(2274, oldSiliconAmount + 2000));
            //            itemStacks.Add(new ItemStack(2273, 2000));
            //            itemStacks.Add(new ItemStack(2274, 2000));

            //            BlueprintResources bps = new BlueprintResources(playerId, itemStacks, false);

            //            SendRequest(Eleon.Modding.CmdId.Request_Blueprint_Resources, Eleon.Modding.CmdId.Request_Blueprint_Resources, bps);
            //        }
            //    }
            //}
        }
    }
}
