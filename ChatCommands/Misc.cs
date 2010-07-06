using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

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
				get { return "Spawn an AI mob (unimplemented)."; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				throw new NotImplementedException();
				//Server.theServ.SpawnMob(sender._player, args);
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
	}
}