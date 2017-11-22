using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCode.ExtensionMethods
{
    public static class Extensions
    {
        public static bool GetIsPrivileged(this Eleon.Modding.PlayerInfo playerInfo)
        {
            // Player = 0, GameMaster = 3, Moderator = 6, Admin = 9 
            return (playerInfo.permission >= 3) && (playerInfo.permission <= 9);
        }
    }
}
