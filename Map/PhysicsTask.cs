using System;

namespace spacecraft
{
	public struct PhysicsTask
	{
		public short x, y, z;
		public Block To;
        public Block From;

		public PhysicsTask(short X, short Y, short Z, Block To)
		{
			x = X; y = Y; z = Z; this.To = To;
            From = Server.theServ.map.GetTile(x, y, z);
		}
        public override int GetHashCode()
        {
            return (x * y * z);
            // Does not include Tile so that two PhysicsTasks concerning the same space will have the same hash.
        }
	}

}