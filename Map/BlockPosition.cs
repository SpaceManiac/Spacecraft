using System;
using System.Collections.Generic;
using System.Text;

/* struct Position
 * contains a set of xDiff, yDiff, zDiff map coordiantes (not world coordinates)
 */

public struct BlockPosition
{
	public short x { get; /* private */ set; }
	public short y { get; /* private */ set; }
	public short z { get; /* private */ set; }

	public BlockPosition(short X, short Y, short Z)
	{
		this = new BlockPosition(); // Using fields in the constructer is messy, so let .NET clear the messiness, then proceed.
		x = X;
		y = Y;
		z = Z;
	}

	static public bool operator ==(BlockPosition A, BlockPosition B)
	{
		return A.Equals(B);
	}
	static public bool operator !=(BlockPosition A, BlockPosition B)
	{
		return !A.Equals(B);
	}

	public override string ToString()
	{
		return "(" + x.ToString() + "," + y.ToString() + "," + z.ToString() + ")";
	}

	public override bool Equals(object obj)
	{
		if (obj is BlockPosition)
		{
			BlockPosition pos = (BlockPosition)obj;
			if (this.x == pos.x &&
				this.y == pos.y &&
				this.z == pos.z)
			{
				return true;
			}
		}
		return false;
	}
	public override int GetHashCode()
	{
		return x.GetHashCode() * y.GetHashCode() * z.GetHashCode();
	}
}
