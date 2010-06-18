using System;

namespace spacecraft
{
	public enum EscherMode {
		None,
		XPlus,
		XMinus,
		YPlus,
		YMinus,
		ZPlus,
		ZMinus
	}

	public static class EscherMath
	{
        // TODO: Fix all this to cast properly, etc. We only need to do that when we actually implement EscherMode.

        /*
		public static Pair<byte[], BlockPosition> MapDataTo(Map map, EscherMode mode)
		{
			byte[] newmap = new byte[map.data.Length];
			BlockPosition dim = new BlockPosition(map.xdim, map.ydim, map.zdim);
			BlockPosition newdim = CoordsTo(map, dim, mode);
			for(short x = 0; x < map.xdim; ++x) {
				for(short y = 0; y < map.ydim; ++y) {
					for(short z = 0; z < map.zdim; ++z) {
						BlockPosition p = new BlockPosition(x, y, z);
						BlockPosition p2 = CoordsTo(map, p, mode);
						newmap[Map.BlockIndex(p2.x, p2.y, p2.z, newdim.x, newdim.z)] =
							map.data[Map.BlockIndex(p.x, p.y, p.z, dim.x, dim.z)];
					}
				}
			}
			return new Pair<byte[], BlockPosition>(newmap, newdim);
		}

		public static BlockPosition CoordsTo(Map map, BlockPosition pos, EscherMode mode)
		{
			int xmax, ymax, zmax;
			if(map == null) {
				xmax = 64; ymax = 64; zmax = 64;
			} else {
				xmax = map.xdim - 1;
				ymax = map.ydim - 1;
				zmax = map.zdim - 1;
			}
			short x = pos.x;
			short y = pos.y;
			short z = pos.z;

			switch(mode) {
			case EscherMode.XPlus:
				return new BlockPosition((short)(ymax - y), x, z);
			case EscherMode.XMinus:
                return new BlockPosition(y, (short)(xmax - x), z);
			case EscherMode.YMinus:
                return new BlockPosition(z, (short)(ymax - y), x);
			case EscherMode.ZPlus:
				return new BlockPosition(y, z, x);
			case EscherMode.ZMinus:
                return new BlockPosition(x, (short)(zmax - z), y);
			case EscherMode.None:
			default:
				return pos;
			}
		}

		public static BlockPosition CoordsFrom(Map map, BlockPosition pos, EscherMode mode)
		{
			int xmax, ymax, zmax;
			if(map == null) {
				xmax = 64; ymax = 64; zmax = 64;
			} else {
				xmax = map.xdim - 1;
				ymax = map.ydim - 1;
				zmax = map.zdim - 1;
			}
			int x = pos.x;
			int y = pos.y;
			int z = pos.z;

			switch(mode) {
			case EscherMode.XPlus:
				return new BlockPosition(y, ymax - x, z);
			case EscherMode.XMinus:
				return new BlockPosition(xmax - y, x, z);
			case EscherMode.YMinus:
				return new BlockPosition(z, ymax - y, x);
			case EscherMode.ZPlus:
				return new BlockPosition(z, x, y);
			case EscherMode.ZMinus:
				return new BlockPosition(x, z, zmax - y);
			case EscherMode.None:
			default:
				return pos;
			}
		}

		public static EscherMode RandomMode()
		{
			return (EscherMode)(1 + (int)(Spacecraft.random.NextDouble() * 5));
		}

		public static EscherMode Invert(EscherMode mode)
		{
			switch(mode) {
			case EscherMode.XPlus:
				return EscherMode.XMinus;
			case EscherMode.XMinus:
				return EscherMode.XPlus;
			case EscherMode.YMinus:
				return EscherMode.YPlus;
			case EscherMode.ZPlus:
				return EscherMode.ZMinus;
			case EscherMode.ZMinus:
				return EscherMode.ZPlus;
			case EscherMode.None:
			default:
				return EscherMode.YMinus;

			}
		}
	*/}
}
