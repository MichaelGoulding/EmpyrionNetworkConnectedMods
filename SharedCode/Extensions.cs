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

        public static System.Numerics.Vector3 ToVector3(this Eleon.Modding.PVector3 vector)
        {
            return new System.Numerics.Vector3(vector.x, vector.y, vector.z);
        }

        public static Eleon.Modding.PVector3 ToPVector3(this System.Numerics.Vector3 vector)
        {
            return new Eleon.Modding.PVector3(vector.X, vector.Y, vector.Z);
        }

        static public bool GreaterOrEqual(this System.Numerics.Vector3 lhs, System.Numerics.Vector3 rhs)
        {
            return ((lhs.X >= rhs.X) && (lhs.Y >= rhs.Y) && (lhs.Z >= rhs.Z));
        }

        static public bool LessOrEqual(this System.Numerics.Vector3 lhs, System.Numerics.Vector3 rhs)
        {
            return ((lhs.X <= rhs.X) && (lhs.Y <= rhs.Y) && (lhs.Z <= rhs.Z));
        }

        // Only format if args are actually passed in to prevent a string passed in from a config using {0} and crashing us.
        static public string SafeFormat(this string format, params object[] args)
        {
            return (args.Length > 0) ? string.Format(format, args) : format;
        }
    }
}
