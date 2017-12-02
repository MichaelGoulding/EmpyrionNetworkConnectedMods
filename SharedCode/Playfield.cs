using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharedCode
{
    public class Playfield : IEquatable<Playfield>
    {
        public string Name { get; private set; }

        public Task RegenerateStructure(int entityId)
        {
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_ConsoleCommand,
                new Eleon.Modding.PString(string.Format("remoteex pf={0} 'regenerate {1}'", ProcessId, entityId)));
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

        //public int type;    // Unknown = 0,Player = 1,BA = 2,CV = 3,SV = 4,HV = 5,AstRes = 6,AstVoxel = 7,EscapePod = 8,Animal = 9,Turret = 10,Item = 11,PlayerDrone = 12,Trader = 13,UndergroundRes=14,
                            // EnemyDrone = 15, PlayerBackpack = 16, DropContainer = 17, ExplosiveDevice = 18, PlayerBike = 19, PlayerBikeFolded = 20, Asteroid = 21, Civilian = 22, Cyborg = 23, TroopTransport = 24
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
