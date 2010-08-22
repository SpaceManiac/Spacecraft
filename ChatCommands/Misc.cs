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
				sender.ResendMap();
			}
		}
	}
}