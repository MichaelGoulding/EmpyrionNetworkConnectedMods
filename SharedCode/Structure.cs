using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCode
{
    public class Structure : Entity
    {
        public Playfield Playfield { get; private set; }

        public Task FinishBlueprint()
        {
            return _gameServerConnection.SendRequest(Eleon.Modding.CmdId.Request_Structure_Touch, new Eleon.Modding.Id(EntityId));
        }

        public Task Regenerate()
        {
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_ConsoleCommand,
                new Eleon.Modding.PString(string.Format("remoteex pf={0} 'regenerate {1}'", Playfield.ProcessId, EntityId)));
        }

        internal Structure(GameServerConnection gameServerConnection, Playfield playfield, Eleon.Modding.GlobalStructureInfo info)
            : base(gameServerConnection, info.id, info.name)
        {
            Playfield = playfield;
        }
    }
}
