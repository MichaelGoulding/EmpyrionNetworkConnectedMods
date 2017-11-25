using System;
using System.Collections.Generic;

namespace SharedCode
{
    public class Playfield : IEquatable<Playfield>
    {
        public string Name { get; private set; }

        public void RegenerateStructure(int entityId)
        {
            _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_ConsoleCommand,
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

        internal Playfield(GameServerConnection gameServerConnection, string name)
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

        private GameServerConnection _gameServerConnection;
    }

    // onPlayerEnteredPlayfield

}
