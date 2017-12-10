using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmpyrionModApi
{
    public interface IGameMod
    {
        void Start(IGameServerConnection gameServerConnection);

        void Stop();
    }
}
