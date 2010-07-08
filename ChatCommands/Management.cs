using System;
using System.Diagnostics;

namespace spacecraft {
	namespace ChatCommands {
		public class Kick : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Mod; }
			}

			public override string HelpMsg
			{
				get { return "Kick a player."; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				if (args == "")
				{
					sender.PrintMessage(Color.CommandError + "No player specified");
				}
				else
				{
					string pname = args;
					Player c = Server.theServ.GetPlayer(pname);
					if (c == null)
					{
						sender.PrintMessage(Color.CommandError + "No such player " + pname);
					}
					else
					{
						c.Kick("You were kicked by " + sender.name);
						Spacecraft.Log(sender.name + " kicked " + c.name);
					}
				}
			}
		}

		public class Broadcast : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Mod; }
			}

			public override string HelpMsg
			{
				get { return "Broadcast a message in yellow text."; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				if (args == "")
				{
					sender.PrintMessage(Color.CommandError + "No message specified");
				}
				else
				{
					Spacecraft.Log("{" + sender.name + "} " + args);
					Server.theServ.MessageAll(Color.Announce + args);
				}
			}
		}

		public class Dehydrate : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Mod; }
			}

			public override string HelpMsg
			{
				get { return "Remove all active water and lava from the map."; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				Server.theServ.map.Dehydrate();
				Spacecraft.Log(sender.name + " dehydrated the map");
			}
		}
		
		public class SaveMap : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Admin; }
			}

			public override string HelpMsg
			{
				get { return "Saves the current map to disk."; }
			}

			public override void Run(Player sender, string cmd, string arg)
			{
				Server.theServ.map.Save(Map.levelName);
				Spacecraft.Log(sender.name + " saved the map.");
			}
		}
		
		public class Physics : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Mod; }
			}

			public override string HelpMsg
			{
				get { return "Get or change physics status."; }
			}

			public override void Run(Player sender, string cmd, string arg)
			{
				if (arg != "") {
					Server.theServ.map.PhysicsOn = Config.StrIsTrue(arg);
					Server.theServ.MessageAll(Color.Announce + "Physics running is now " + Server.theServ.map.PhysicsOn.ToString());
				} else {
					sender.PrintMessage(Color.CommandResult + "Physics running is " + Server.theServ.map.PhysicsOn.ToString());
				}
			}
		}

		public class SetSpawn : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Admin; }
			}

			public override string HelpMsg
			{
				get { return "Set the map's global spawn point"; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				Server.theServ.map.SetSpawn(sender.pos, sender.heading);
				sender.PrintMessage(Color.CommandResult + "Spawn point set");
				Spacecraft.Log(sender.name + " set the spawn point");
			}
		}

		public class Exit : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Admin; }
			}

			public override string HelpMsg
			{
				get { return "Gracefully shut down the server"; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				Spacecraft.Log(sender.name + " shut down the server (/exit command)");
				//Server.theServ.SendAll(Player.PacketKick("Server is shutting down!"));
				Server.OnExit.Set();
			}
		}
		
		public class Configure : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Admin; }
			}

			public override string HelpMsg
			{
				get { return "View raw server configuration (possibly edit, later)."; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				string[] argv = args.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (argv.Length == 0)
				{
					string list = "Options set:" + Config.DefinedList();
					ChatCommandHandling.WrapMessage(sender, list);
				}
				else if (argv.Length == 1)
				{
					string r = Config.Get(argv[0], null);
					if (r == null)
					{
						sender.PrintMessage(Color.CommandResult + "No option " + argv[0] + " defined");
					}
					else
					{
						sender.PrintMessage(Color.CommandResult + "Option " + argv[0] + " is " + r);
					}
				}
				else if (argv.Length == 2)
				{
					// TODO: possibly allow setting config inline
				}
			}
		}
		
		public class ExecuteTcl : ChatCommandBase
		{
			public override Rank RankNeeded {
				get { return Rank.Admin; }
			}
			
			public override string HelpMsg {
				get { return "Execute Tcl code."; }
			}
			
			public override void Run(Player sender, string cmd, string args)
			{
				Spacecraft.Log(sender.name + " executed Tcl: " + args);
				int status = Scripting.Interpreter.EvalScript(args);
				string commandResult = Scripting.Interpreter.Result;
				if (!Scripting.IsOk(status)) {
					if(commandResult == "") {
						commandResult = "Unknown error!";
					}
					commandResult = Color.CommandError + commandResult;
				}
				if(commandResult != "") {
					sender.PrintMessage(Color.CommandResult + "Result: " + commandResult);
				}
			}
		}
		
		public class Diagnostics : ChatCommandBase
		{
			public override Rank RankNeeded {
				get { return Rank.Mod; }
			}
			
			public override string HelpMsg {
				get { return "Display some diagnostic information on physics and server usage."; }
			}
			
			public override void Run(Player sender, string cmd, string arg)
			{
				TimeSpan up = (DateTime.Now - Process.GetCurrentProcess().StartTime);
				string Uptime = "";
				if(up.Days > 0) {
					Uptime += up.Days + " days ";
				}
				if(up.Hours > 0) {
					Uptime += up.Hours + " hours ";
				}
				if(up.Minutes > 0) {
					Uptime += up.Minutes + " minutes ";
				}
				if(up.Seconds > 0) {
					Uptime += up.Seconds + " seconds ";
				}
				
				Process p = Process.GetCurrentProcess();
				Server s = Server.theServ;
				
#if WIN32
				double cpu = Spacecraft.cpuCounter.NextValue();
				cpu = Math.Round(10 * cpu) / 10.0;
#endif
				
				double ram = p.PagedMemorySize64 / (1024.0 * 1024.0);
				ram = Math.Round(10 * ram) / 10.0;
				
				sender.PrintMessage(Color.CommandResult + "Spacecraft @ " + s.IP + ":" + s.port + " (http: " + HttpMonitor.port + ")");
				sender.PrintMessage(Color.CommandResult + "Players online: " + s.Players.Count);
				sender.PrintMessage(Color.CommandResult + "ActiveList length: " + s.map.ActiveListLength + " - Updates last tick: " + s.map.UpdatedLastTick);
				sender.PrintMessage(Color.CommandResult + "Server uptime: " + Uptime);
				sender.PrintMessage(Color.CommandResult + "Last heartbeat took: " + s.LastHeartbeatTook + "s - Last physics tick took: " + s.LastPhysicsTickTook + "s");
				try {
					sender.PrintMessage(Color.CommandResult + "CPU usage: " + cpu + "% - RAM usage: " + ram + "Mb");
				}
				catch(NotImplementedException) {
					sender.PrintMessage(Color.CommandResult + "CPU/RAM usage unavailable under Mono");
				}
			}
		}
	}
}