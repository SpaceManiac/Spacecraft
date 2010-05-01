using System;
using System.Collections.Generic;
using System.Text;

namespace spacecraft
{
    struct Position
    {
        public Int16 x { get; private set; }
        public Int16 y { get; private set; }
        public Int16 z { get; private set; }

        public Position(Int16 X, Int16 Y, Int16 Z)
        {
            this = new Position(); // Using fields in the constructer is messy, so let .NET clear the messiness, then proceed.
            x = X;
            y = Y;
            z = Z;
        }

        public Position(byte x, byte y, byte z)
        {
            this = new Position((Int16)x, (Int16)y, (Int16)z);
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
