using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmpyrionModApi
{
    public class Structure : Entity
    {
        public int Class { get; private set; }

        public Player Pilot { get; private set; }

        public Task FinishBlueprint()
        {
            return _gameServerConnection.SendRequest(Eleon.Modding.CmdId.Request_Structure_Touch, new Eleon.Modding.Id(EntityId));
        }

        internal Structure(IGameServerConnection gameServerConnection, Playfield playfield, Eleon.Modding.GlobalStructureInfo info)
            : base(gameServerConnection, info.id, (EntityType)info.type, info.name)
        {
            Class = info.classNr;
            this.Position = new WorldPosition(playfield, new Vector3(info.pos), new Vector3(info.rot));
            this.MemberOfFaction = _gameServerConnection.GetFaction(info.factionId);
            this.Pilot = (info.pilotId == 0) ? null : _gameServerConnection.GetOnlinePlayers()[info.pilotId];
        }
    }
}
