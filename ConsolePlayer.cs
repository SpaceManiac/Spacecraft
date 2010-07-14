using System;
using System.Threading;

namespace spacecraft
{
	// ConsolePlayer is an auxilary class that makes it possible for commands to be
	// evaluated from the console. Messages printed are directed to the console.
	
	public class ConsolePlayer : Player
	{
		public ConsolePlayer() : base(null)
		{
			rank = Rank.Admin;
			name = "[console]";
			playerID = 255;
			pos = Server.theServ.map.spawn;
			heading = 0;
			pitch = 0;
		}
		
		public new void Start()
		{
			Thread T = new Thread(ReadConsoleThread, Spacecraft.StackSize);
			T.Name = "ConsoleRead";
			T.Start();
		}
		
		private void ReadConsoleThread()
		{
			while(true) {
				Thread.Sleep(10);
				try
				{
					string line = Console.ReadLine();
					if(line == null) continue;
					HandleMessage(line);
				}
				catch(Exception e) {
					Spacecraft.LogError("couldn't read from the console (probably tried to read empty line)", e);
					//return;
				}
			}
		}
		
		public override void Kick(string reason)
		{
			Spacecraft.Log("[-] Console \"kicked\" (" + reason + ")");
		}
		
		public override void PrintMessage(string msg)
		{
			Spacecraft.Log("[-] " + msg);
		}

		public override void PlayerJoins(Player Player)
		{
			// do nothing
		}

		public override void PlayerMoves(Player Player, Position dest, byte heading, byte pitch)
		{
			// do nothing
		}

		public override void PlayerDisconnects(byte ID)
		{
			// do nothing
		}

		public override void BlockSet(BlockPosition pos, Block type)
		{
			// do nothing
		}
	}
}