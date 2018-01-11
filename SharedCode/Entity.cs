using EmpyrionModApi.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EmpyrionModApi
{
    public class Entity : IEquatable<Entity>
    {
        public enum EntityType
        {
            Unknown = 0,
            Player = 1,
            BA = 2,
            CV = 3,
            SV = 4,
            HV = 5,
            AstRes = 6,
            AstVoxel = 7,
            EscapePod = 8,
            Animal = 9,
            Turret = 10,
            Item = 11,
            PlayerDrone = 12,
            Trader = 13,
            UndergroundRes = 14,
            EnemyDrone = 15,
            PlayerBackpack = 16,
            DropContainer = 17,
            ExplosiveDevice = 18,
            PlayerBike = 19,
            PlayerBikeFolded = 20,
            Asteroid = 21,
            Civilian = 22,
            Cyborg = 23,
            TroopTransport = 24
        }

        public int EntityId
        {
            get
            {
                return _entityId;
            }
        }

        public byte Origin { get; protected set; }

        public byte FactionGroupId { get; protected set; }

        public EntityType Type { get; private set; }

        public string Name { get; protected set; }

        public string DisplayName
        {
            get
            {
                if (MemberOfFaction != null)
                {
                    return $"[{MemberOfFaction.Initials}] {Name}";
                }
                else
                {
                    return Name;
                }
            }
        }

        public WorldPosition Position { get; protected set; }

        public Faction MemberOfFaction { get; protected set; }

        public int FactionIdOrEntityId
        {
            get
            {
                if (MemberOfFaction != null)
                {
                    return MemberOfFaction.Id;
                }
                else
                {
                    return EntityId;
                }
            }
        }

        public Task Teleport(Vector3 newPosition, Vector3 newRotation)
        {
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_Entity_Teleport,
                new Eleon.Modding.IdPositionRotation(EntityId, newPosition.ToPVector3(), newRotation.ToPVector3()));
        }

        public Task ChangePlayfield(WorldPosition newWorldPosition)
        {
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_Entity_ChangePlayfield,
                new Eleon.Modding.IdPlayfieldPositionRotation(
                    EntityId,
                    newWorldPosition.playfield.Name,
                    newWorldPosition.position.ToPVector3(),
                    newWorldPosition.rotation.ToPVector3()));
        }

        public Task Destroy()
        {
            return _gameServerConnection.SendRequest(Eleon.Modding.CmdId.Request_Entity_Destroy, new Eleon.Modding.Id(EntityId));
        }

        public Task ChangeFaction(Faction faction)
        {
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_ConsoleCommand,
                new Eleon.Modding.PString(string.Format("remoteex pf={0} 'faction entity {1} {2}'", Position.playfield.ProcessId, faction.Id, EntityId )));
        }

        //Request_Entity_Destroy2,            // IdPlayfield (id of entity, playfield the entity is in)
        //Request_Entity_Export,              // EntityExportInfo
        //Request_Entity_SetName,             // IdPlayfieldName (if playfield == null we try to find the corresponding playfield, playfield must be loaded)


        public Task GetCurrentPosition()
        {
            return _gameServerConnection.SendRequest<Eleon.Modding.IdPositionRotation>(Eleon.Modding.CmdId.Request_Entity_PosAndRot, new Eleon.Modding.Id(EntityId))
                .ContinueWith((task) =>
                {
                    var newData = task.Result;

                    lock (this)
                    {
                        Position = new WorldPosition(Position.playfield, newData.pos.ToVector3(), newData.rot.ToVector3());
                    }
                });
        }


        public Task Regenerate()
        {
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_ConsoleCommand,
                new Eleon.Modding.PString(string.Format("remoteex pf={0} 'regenerate {1}'", Position.playfield.ProcessId, EntityId)));
        }

        #region Internal Methods

        internal void UpdateInfo(Playfield playfield)
        {
            this.Position = new WorldPosition(playfield, this.Position.position, this.Position.rotation);
        }

        #endregion

        #region Protected methods

        protected Entity(IGameServerConnection gameServerConnection, int entityId, EntityType type, string name)
        {
            _gameServerConnection = gameServerConnection;
            _entityId = entityId;
            Type = type;
            Name = name;
        }

        #endregion

        #region Protected Data

        protected IGameServerConnection _gameServerConnection;

        #endregion

        #region Private Data

        private int _entityId;

        #endregion

        #region Common overloads

        public override string ToString()
        {
            return string.Format("{0}({1})", Name, EntityId );
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            Entity e = obj as Entity;
            if (e == null)
            {
                return false;
            }

            return (_entityId == e._entityId);
        }

        public bool Equals(Entity other)
        {
            return other != null &&
                   _entityId == other._entityId;
        }

        public override int GetHashCode()
        {
            return -1485059848 + _entityId.GetHashCode();
        }

        public static bool operator ==(Entity entity1, Entity entity2)
        {
            return EqualityComparer<Entity>.Default.Equals(entity1, entity2);
        }

        public static bool operator !=(Entity entity1, Entity entity2)
        {
            return !(entity1 == entity2);
        }

        #endregion
    }
}
