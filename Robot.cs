
using System;
using System.Collections;
using System.Threading;

namespace spacecraft
{
	public class Robot
	{
		public string name { get; protected set; }
		public Position pos;
		public byte playerID { get; protected set; }
		public byte heading { get; protected set; }
		public byte pitch { get; protected set; }
		
		public delegate void RobotSpawnHandler(Robot sender);
		public event RobotSpawnHandler Spawn;
		
		public delegate void RobotMoveHandler(Robot player, Position dest, byte heading, byte pitch);
		public event RobotMoveHandler Move;
		
		public delegate void RobotDisconnectHandler(Robot sender);
		public event RobotDisconnectHandler Disconnect;
		
		private double time;
		private bool update;

		public Robot(string name)
		{
			this.name = name;
			this.playerID = Player.AssignID();
			pos = new Position((short)(Server.theServ.map.xdim * 16),
				(short)(Server.theServ.map.ydim * 16), (short)(Server.theServ.map.zdim * 16));
			heading = 0;
			pitch = 0;
			time = 0;
			update = true;
		}
		
		~Robot() {
			Player.InUseIDs.Remove(playerID);
		}
		
		public void Start()
		{
			if(Spawn != null) {
				Spawn(this);
			}
		}
		
		public void Stop()
		{
			if(Disconnect != null) {
				Disconnect(this);
			}
		}

		public void Update()
		{
			update = !update;
			if(!update) return;
			
			time += 0.03 / 2;
			if (time >= 6) {
				time = 0;
			}
				
			if (time < 3) {
				pos.x += 1;
				heading = 64;
			} else if (time < 6) {
				pos.x -= 1;
				heading = 196;
			}
			
			if(Move != null) Move(this, pos, heading, pitch);
		}
	}
}
