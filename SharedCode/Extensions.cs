using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmpyrionModApi.ExtensionMethods
{
    public static class Extensions
    {


        public static bool AreTheSame( this Eleon.Modding.ItemStack[] lhs, Eleon.Modding.ItemStack[] rhs )
        {
            if(lhs == null)
            {
                return (rhs == null);
            }
            else if (rhs == null)
            {
                return false;
            }

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
