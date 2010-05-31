using System;
using System.Collections.Generic;
using System.Text;

/* struct Position
 * contains a set of x, y, z world coordinates (not map coordinates)
 */
namespace spacecraft
{
    public struct Position
    {
        public short x { get; /* private */ set; }
        public short y { get; /* private */ set; }
        public short z { get; /* private */ set; }

        public Position(short X, short Y, short Z)
        {
            this = new Position(); // Using fields in the constructer is messy, so let .NET clear the messiness, then proceed.
            x = X;
            y = Y;
            z = Z;
        }

        public Position(byte x, byte y, byte z)
        {
            this = new Position((short)x, (short)y, (short)z);
        }


        static public bool operator ==(Position A, Position B)
        {
            return A.Equals(B);
        }
        static public bool operator !=(Position A, Position B)
        {
            return !A.Equals(B);
        }

        public override bool Equals(object obj)
        {
            bool equal = false;
            if (obj is Position)
            {
                Position pos = (Position)obj;
                if (this.x == pos.x &&
                    this.y == pos.y &&
                    this.z == pos.z)
                {
                    equal = true;
                }
            }
            return equal;
        }
        public override int GetHashCode()
        {
            return x.GetHashCode() * y.GetHashCode() * z.GetHashCode();
        }
    }
}