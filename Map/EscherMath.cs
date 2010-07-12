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
		public static Pair<byte[], BlockPosition> MapDataTo(MapTag map, EscherMode mode)
		{
			byte[] newmap = new byte[map.data.Length];
			BlockPosition dim = new BlockPosition(map.xdim, map.ydim, map.zdim);
			BlockPosition newdim = CoordsTo(map, dim, mode);
			for(short xDiff = 0; xDiff < map.xdim; ++xDiff) {
				for(short yDiff = 0; yDiff < map.ydim; ++yDiff) {
					for(short zDiff = 0; zDiff < map.zdim; ++zDiff) {
						BlockPosition p = new BlockPosition(xDiff, yDiff, zDiff);
						BlockPosition p2 = CoordsTo(map, p, mode);
						newmap[MapTag.BlockIndex(p2.xDiff, p2.yDiff, p2.zDiff, newdim.xDiff, newdim.zDiff)] =
							map.data[MapTag.BlockIndex(p.xDiff, p.yDiff, p.zDiff, dim.xDiff, dim.zDiff)];
					}
				}
			}
			return new Pair<byte[], BlockPosition>(newmap, newdim);
		}

		public static BlockPosition CoordsTo(MapTag map, BlockPosition pos, EscherMode mode)
		{
			int xmax, ymax, zmax;
			if(map == null) {
				xmax = 64; ymax = 64; zmax = 64;
			} else {
				xmax = map.xdim - 1;
				ymax = map.ydim - 1;
				zmax = map.zdim - 1;
			}
			short xDiff = pos.xDiff;
			short yDiff = pos.yDiff;
			short zDiff = pos.zDiff;

			switch(mode) {
			case EscherMode.XPlus:
				return new BlockPosition((short)(ymax - yDiff), xDiff, zDiff);
			case EscherMode.XMinus:
	return new BlockPosition(yDiff, (short)(xmax - xDiff), zDiff);
			case EscherMode.YMinus:
	return new BlockPosition(zDiff, (short)(ymax - yDiff), xDiff);
			case EscherMode.ZPlus:
				return new BlockPosition(yDiff, zDiff, xDiff);
			case EscherMode.ZMinus:
	return new BlockPosition(xDiff, (short)(zmax - zDiff), yDiff);
			case EscherMode.None:
			default:
				return pos;
			}
		}

		public static BlockPosition CoordsFrom(MapTag map, BlockPosition pos, EscherMode mode)
		{
			int xmax, ymax, zmax;
			if(map == null) {
				xmax = 64; ymax = 64; zmax = 64;
			} else {
				xmax = map.xdim - 1;
				ymax = map.ydim - 1;
				zmax = map.zdim - 1;
			}
			int xDiff = pos.xDiff;
			int yDiff = pos.yDiff;
			int zDiff = pos.zDiff;

			switch(mode) {
			case EscherMode.XPlus:
				return new BlockPosition(yDiff, ymax - xDiff, zDiff);
			case EscherMode.XMinus:
				return new BlockPosition(xmax - yDiff, xDiff, zDiff);
			case EscherMode.YMinus:
				return new BlockPosition(zDiff, ymax - yDiff, xDiff);
			case EscherMode.ZPlus:
				return new BlockPosition(zDiff, xDiff, yDiff);
			case EscherMode.ZMinus:
				return new BlockPosition(xDiff, zDiff, zmax - yDiff);
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
