using System;

namespace spacecraft.AirshipWars
{
	public class Airship
	{
		private bool destroyed;
		public AirshipWarsTeam team;
		public short x, y, z;
		
		public Airship(AirshipWarsTeam team, short X, short Y, short Z) {
			destroyed = false;
			this.team = team;
			x = X; y = Y; z = Z;
			Draw();
		}
		
		public void Destroy() {
			if (destroyed) return;
			Erase();
			destroyed = true;
		}
		
		~Airship() {
			Destroy();
		}
		
		private void Draw() {
			Map map = Server.theServ.map;
			map.SetTile(x, y, z, Block.CoalOre);
		}
		
		private void Erase() {
			Map map = Server.theServ.map;
			map.SetTile(x, y, z, Block.Air);
		}
		
		public void Move(short X, short Y, short Z) {
			Erase();
			x += X; y += Y; z += Z;
			Draw();
		}
	}
}
