﻿using EmpyrionModApi.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace EmpyrionModApi
{
    public class Playfield : IEquatable<Playfield>
    {
        public string Name { get; private set; }

        public Dictionary<int, Structure> StructuresById { get { return _structuresById; } }

        public async Task RefreshStructureList()
        {
            var result = await _gameServerConnection.SendRequest<Eleon.Modding.GlobalStructureList>(
                Eleon.Modding.CmdId.Request_GlobalStructure_Update,
                new Eleon.Modding.PString(Name));

            UpdateInfo(result);
        }

        public Task RegenerateStructure(int entityId)
        {
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_ConsoleCommand,
                new Eleon.Modding.PString(string.Format("remoteex pf={0} 'regenerate {1}'", ProcessId, entityId)));
        }

        const byte k_factionGroupId_Faction = 0;
        const byte k_factionGroupId_Private = 1;

        public async Task SpawnEntity(string name, Entity.EntityType type, string prefabName, Vector3 position, Faction faction)
        {
            var newId = await _gameServerConnection.SendRequest<Eleon.Modding.Id>(Eleon.Modding.CmdId.Request_NewEntityId, null);

            var spawnInfo = new Eleon.Modding.EntitySpawnInfo();
            spawnInfo.forceEntityId = newId.id;
            spawnInfo.playfield = this.Name;
            spawnInfo.pos = position.ToPVector3();
            spawnInfo.rot = new Eleon.Modding.PVector3();
            spawnInfo.name = name;
            spawnInfo.type = (byte)type;
            spawnInfo.prefabName = prefabName;
            spawnInfo.factionGroup = k_factionGroupId_Faction;
            spawnInfo.factionId = faction.Id;
            //spawnInfo.exportedEntityDat = exportFile;

            await _gameServerConnection.SendRequest(Eleon.Modding.CmdId.Request_Entity_Spawn, spawnInfo);
        }


        public async Task SpawnEntity(string name, Entity.EntityType type, string prefabName, Vector3 position, Player player)
        {
            var newId = await _gameServerConnection.SendRequest<Eleon.Modding.Id>(Eleon.Modding.CmdId.Request_NewEntityId, null);

            var spawnInfo = new Eleon.Modding.EntitySpawnInfo();
            spawnInfo.forceEntityId = newId.id;
            spawnInfo.playfield = this.Name;
            spawnInfo.pos = position.ToPVector3();
            spawnInfo.rot = new Eleon.Modding.PVector3();
            spawnInfo.name = name;
            spawnInfo.type = (byte)type;
            spawnInfo.prefabName = prefabName;
            spawnInfo.factionGroup = k_factionGroupId_Private;
            spawnInfo.factionId = player.EntityId;
            //spawnInfo.exportedEntityDat = exportFile;

            _gameServerConnection.DebugOutput("Creating entity for '{0}' of faction group '{1}', entity id '{2}' ", player, spawnInfo.factionGroup, spawnInfo.factionId);

            await _gameServerConnection.SendRequest(Eleon.Modding.CmdId.Request_Entity_Spawn, spawnInfo);
        }

        internal int ProcessId { get; private set; }

        #region Common overloads

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            if (!(obj is Playfield))
            {
                return false;
            }

            Playfield p = obj as Playfield;

            // Return true if the Name fields match:
            return (Name == p.Name);
        }

        public bool Equals(Playfield other)
        {
            return other != (Playfield)null &&
                   Name == other.Name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override int GetHashCode()
        {
            return -1125283371 + EqualityComparer<string>.Default.GetHashCode(Name);
        }

        public static bool operator ==(Playfield playfield1, Playfield playfield2)
        {
            return EqualityComparer<Playfield>.Default.Equals(playfield1, playfield2);
        }

        public static bool operator !=(Playfield playfield1, Playfield playfield2)
        {
            return !(playfield1 == playfield2);
        }

        public static bool operator ==(Playfield playfield1, string playfield2)
        {
            return (playfield1.Name == playfield2);
        }

        public static bool operator !=(Playfield playfield1, string playfield2)
        {
            return (playfield1.Name != playfield2);
        }

        #endregion

        internal Playfield(IGameServerConnection gameServerConnection, string name)
        {
            _gameServerConnection = gameServerConnection;
            Name = name;
        }

        internal void UpdateInfo(Eleon.Modding.PlayfieldLoad playfieldLoadData)
        {
            Name = playfieldLoadData.playfield;
            ProcessId = playfieldLoadData.processId;
        }

        internal void UpdateInfo(Eleon.Modding.PlayfieldStats playfieldStats)
        {
            Name = playfieldStats.playfield;
            ProcessId = playfieldStats.processId;
        }

        internal void UpdateInfo(Eleon.Modding.PlayfieldEntityList playfieldEntityList)
        {
            System.Diagnostics.Debug.Assert(playfieldEntityList.playfield == Name);
        }

        internal void UpdateInfo(Eleon.Modding.GlobalStructureList globalStructureList)
        {
            var listForPlayfield = globalStructureList.globalStructures[Name];

            lock (_structuresById)
            {
                _structuresById.Clear();

                foreach (var structInfo in listForPlayfield)
                {
                    _structuresById.Add(structInfo.id, new Structure(_gameServerConnection, this, structInfo));
                }
            }
        }

        private IGameServerConnection _gameServerConnection;

        private Dictionary<int, Structure> _structuresById = new Dictionary<int, Structure>();
    }

    // onPlayerEnteredPlayfield

}
