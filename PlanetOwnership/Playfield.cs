using System;
using System.Collections.Generic;

namespace PlanetOwnership
{
    class Playfield : IEquatable<Playfield>
    {
        string _name;

        public Playfield(string name)
        {
            _name = name;
        }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Playfield p = obj as Playfield;
            if (p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (_name == p._name);
        }

        public bool Equals(Playfield other)
        {
            return other != null &&
                   _name == other._name;
        }

        public override int GetHashCode()
        {
            return -1125283371 + EqualityComparer<string>.Default.GetHashCode(_name);
        }

        public static bool operator ==(Playfield playfield1, Playfield playfield2)
        {
            return EqualityComparer<Playfield>.Default.Equals(playfield1, playfield2);
        }

        public static bool operator !=(Playfield playfield1, Playfield playfield2)
        {
            return !(playfield1 == playfield2);
        }
    }

    // onPlayerEnteredPlayfield

}
