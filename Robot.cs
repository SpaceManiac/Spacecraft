/*
using System;
using System.Collections;

namespace spacecraft
{
	public class Robot : Player
	{
		private double time;

		public Robot(string name)
			: base(name)
		{
			time = 0;
		}

		public void Update()
		{
			//MinecraftServer serv = MinecraftServer.theServ;

			time += (1.0 / 30);
			if (time < 1)
			{
				xDiff += 6;
			}
			else if (time < 2)
			{
				zDiff += 6;
			}
			else if (time < 3)
			{
				xDiff -= 6;
			}
			else if (time < 4)
			{
				zDiff -= 6;
			}
			else
			{
				time = 0;
				//Update(serv);
			}
			//serv.SendAll(Connection.PacketPositionUpdate(this));
		}
	}
}
*/