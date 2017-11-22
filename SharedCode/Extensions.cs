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

        public static bool AreTheSame( this Eleon.Modding.ItemStack[] lhs, Eleon.Modding.ItemStack[] rhs )
        {
            if (lhs.Length != rhs.Length)
            {
                return false;
            }

            for(int i = 0; i < lhs.Length; ++i)
            {
                if((lhs[i].id != rhs[i].id) || (lhs[i].count != rhs[i].count))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
