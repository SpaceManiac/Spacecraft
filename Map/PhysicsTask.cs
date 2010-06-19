using System;
using System.Text;

namespace spacecraft
{
	public struct PhysicsTask : IComparable
	{
		public short x, y, z;
		public Block To;
	//public Block From;

		public PhysicsTask(short X, short Y, short Z, Block To)
		{
			x = X; y = Y; z = Z; this.To = To;
	//From = Server.theServ.map.GetTile(xDiff, yDiff, zDiff);
		}
	public override int GetHashCode()
	{
	return (x * y * z);
	// Does not include Tile so that two PhysicsTasks concerning the same space will have the same hash.
	}

	public int CompareTo(object obj)
	{
	PhysicsTask P = (PhysicsTask)obj;
	if (this.To > P.To)
	{ return 1; }
	else if (this.To == P.To)
	{ return 0; }
	else
	{ return -1; }
	}

	public override string ToString()
	{
	StringBuilder b = new StringBuilder();
	b.Append(To.ToString());
	b.Append(" ");
	b.Append("(");
	b.Append(x.ToString());
	b.Append(",");
	b.Append(y.ToString());
	b.Append(",");
	b.Append(z.ToString());
	b.Append(")");
	return b.ToString();
	}
	}

}