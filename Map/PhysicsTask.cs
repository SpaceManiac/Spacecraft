using System;

namespace spacecraft
{
	public struct PhysicsTask
	{
		public short x, y, z;
		public Block tile;

		public PhysicsTask(short X, short Y, short Z, Block Tile)
		{
			x = X; y = Y; z = Z; tile = Tile;
		}
	}

}