using System;
using System.Collections.Generic;

namespace spacecraft {
	namespace ChatCommands {
		public class Summon : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Mod; }
			}

			public override string HelpMsg
			{
				get { return "Teleport a player to you."; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				if (args == "")
				{
					sender.PrintMessage(Color.CommandError + "No player specified");
				}
				else
				{
					Player p = Server.theServ.GetPlayer(args);
					if (p == null) {
						sender.PrintMessage(Color.CommandError + "No such player " + args);
					} else {
						Server.theServ.MovePlayer(p, sender.pos, sender.heading, sender.pitch);
						p.PrintMessage(Color.PrivateMsg + sender.name + " summoned you!");
						Spacecraft.Log(sender.name + " summon " + p.name);
					}
				}
			}
		}

		public class Teleport : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Builder; }
			}

			public override string HelpMsg
			{
				get { return "Teleport to a player."; }
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
					Player p = Server.theServ.GetPlayer(pname);
					if (p == null)
					{
						sender.PrintMessage(Color.CommandError + "No such player " + pname);
					}
					else
					{
						Server.theServ.MovePlayer(sender, p.pos, p.heading, p.pitch);
						Spacecraft.Log(sender.name + " telported to " + p.name);
					}
				}
			}
		}

		public class SpawnMob : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Mod; }
			}

			public override string HelpMsg
			{
				get { return "Spawn an AI mob."; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				string[] argv = args.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
				if(argv.Length != 2) {
					sender.PrintMessage(Color.CommandError + "Usage: /mob action param");
					return;
				}
				string action = argv[0];
				string param = argv[1];
				if(action == "actions") {
					sender.PrintMessage(Color.CommandResult + "Actions: spawn kill");
				} else if(action == "spawn") {
					Server.theServ.SpawnRobot(sender.pos, param);
				} else if(action == "kill") {
					foreach(Robot r in new List<Robot>(Server.theServ.Robots)) {
						if(r.name == param || param == "*") {
							r.Stop();
						}
					}
				}
			}
		}

		public class ResendMap : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Mod; }
			}

			public override string HelpMsg
			{
				get { return "Resend the map for testing purposes (unimplemented)."; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				throw new NotImplementedException();
				//sender.ResendMap();
			}
		}
		
		public class ExecuteTcl : ChatCommandBase
		{
			Rank _rankNeeded;
			string _script;
			string _helpMsg;
			
			public override Rank RankNeeded {
				// fool the documentation script
				// get { return Rank.Admin; }
				get { return _rankNeeded; }
			}
			
			public override string HelpMsg {
				// fool the documentation script
				// get { return "Execute Tcl code."; }
				get { return _helpMsg; }
			}
			
			public ExecuteTcl()
			{
				// default constructor: this is being added as the normal /tcl command
				_rankNeeded = Rank.Admin;
				_script = "";
				_helpMsg = "Execute Tcl code.";
			}
			
			public ExecuteTcl(string rankNeeded, string help, string script)
			{
				// special constructor: this is a Tcl command
				_rankNeeded = (Rank) Enum.Parse(typeof(Rank), rankNeeded.ToLower(), true);
				_script = script;
				_helpMsg = help;
			}
			
			public override void Run(Player sender, string cmd, string args)
			{
				if(_script == "") {
					// Normal, /tcl command
					Spacecraft.Log(sender.name + " executed Tcl: " + args);
					int status = Scripting.Interpreter.EvalScript(args);
					string result = Scripting.Interpreter.Result;
					if (Scripting.IsOk(status)) {
						if(result != "") {
							ChatCommandHandling.WrapMessage(sender, result);
						}
					} else {
						ChatCommandHandling.WrapMessage(sender, "Tcl error: " + Scripting.Interpreter.Result, Color.CommandError);
					}
				} else {
					// It's a Tcl-defined command.
					// it'll be called along the lines of "script SpaceManiac argument argument"
					int status = Scripting.Interpreter.EvalScript(_script + " " + sender.name + " " + args);
					string result = Scripting.Interpreter.Result;
					if (Scripting.IsOk(status)) {
						if(result != "") {
							ChatCommandHandling.WrapMessage(sender, result);
						}
					} else {
						ChatCommandHandling.WrapMessage(sender, "Tcl error: " + Scripting.Interpreter.Result, Color.CommandError);
					}
				}
			}
		}
	}
}