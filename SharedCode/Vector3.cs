using System;

namespace EmpyrionModApi
{
    public struct Vector3 : IEquatable<Vector3>
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        internal Vector3(Eleon.Modding.PVector3 pos)
        {
            this.x = pos.x;
            this.y = pos.y;
            this.z = pos.z;
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", x, y, z);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Vector3))
            {
                return false;
            }

            Vector3 rhs = (Vector3)obj;
            return ((x == rhs.x) && (y == rhs.y) && (z == rhs.z));
        }

        public bool Equals(Vector3 other)
        {
            return x == other.x &&
                   y == other.y &&
                   z == other.z;
        }

        public override int GetHashCode()
        {
            var hashCode = 373119288;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            return hashCode;
        }

        static public bool operator >=(Vector3 lhs, Vector3 rhs)
        {
            return ((lhs.x >= rhs.x) && (lhs.y >= rhs.y) && (lhs.z >= rhs.z));
        }

        static public bool operator <=(Vector3 lhs, Vector3 rhs)
        {
            return ((lhs.x <= rhs.x) && (lhs.y <= rhs.y) && (lhs.z <= rhs.z));
        }

        public static bool operator ==(Vector3 vector1, Vector3 vector2)
        {
            return vector1.Equals(vector2);
        }

        public static bool operator !=(Vector3 vector1, Vector3 vector2)
        {
            return !(vector1 == vector2);
        }

        public static Vector3 operator +(Vector3 vector1, Vector3 vector2)
        {
            return new Vector3((vector1.x + vector2.x), (vector1.y + vector2.y), (vector1.z + vector2.z));
        }

        public static implicit operator Eleon.Modding.PVector3(Vector3 vector)
        {
            return new Eleon.Modding.PVector3(vector.x, vector.y, vector.z);
        }
    }
}
