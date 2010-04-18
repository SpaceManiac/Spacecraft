using System;

public struct PositionBlock
{
	public short x, y, z;
	public byte tile;
	
	public PositionBlock (short X, short Y, short Z, byte Tile) {
		x = X; y = Y; z = Z; tile = Tile;
	}
}
