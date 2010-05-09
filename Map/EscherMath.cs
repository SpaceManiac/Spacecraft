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
		public static byte[] MapDataTo(byte[] map, EscherMode mode) {
			return map;
		}
		
		public static BlockPosition CoordsTo(BlockPosition pos, EscherMode mode) {
			if(mode == EscherMode.None || mode == EscherMode.YPlus) return pos;
			// magic!
			return pos;
		}
		
		public static BlockPosition CoordsFrom(BlockPosition pos, EscherMode mode) {
			if(mode == EscherMode.None || mode == EscherMode.YPlus) return pos;
			// reverse magic!
			return pos;
		}
	}
}
