using EmpyrionModApi.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace EmpyrionModApi
{
    public struct Rect3 : IEquatable<Rect3>
    {
        public Vector3 pt0;
        public Vector3 pt1;

        public Rect3(Vector3 pt0, Vector3 pt1)
        {
            this.pt0 = pt0;
            this.pt1 = pt1;
        }

        public bool Contains(Vector3 pt)
        {
            return (pt.GreaterOrEqual(pt0) && pt.LessOrEqual(pt1));
        }

        public bool Equals(Rect3 other)
        {
            return EqualityComparer<Vector3>.Default.Equals(pt0, other.pt0) &&
                   EqualityComparer<Vector3>.Default.Equals(pt1, other.pt1);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Rect3))
            {
                return false;
            }

            Rect3 rhs = (Rect3)obj;
            return ((pt0 == rhs.pt0) && (pt1 == rhs.pt1));
        }

        public override int GetHashCode()
        {
            var hashCode = -1548350677;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(pt0);
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(pt1);
            return hashCode;
        }

        public static bool operator ==(Rect3 rect1, Rect3 rect2)
        {
            return rect1.Equals(rect2);
        }

        public static bool operator !=(Rect3 rect1, Rect3 rect2)
        {
            return !(rect1 == rect2);
        }
    }

    // onPlayerEnteredPlayfield

}
