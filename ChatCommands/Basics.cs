using System;

namespace spacecraft {
	namespace ChatCommands {
		public class Help : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Guest; }
			}

			public override string HelpMsg
			{
				get { return "Get general help, a list of commands, or help on a specific command"; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				if (args == "")
				{
					sender.PrintMessage(Color.CommandResult + "You are a " + RankInfo.RankColor(sender.rank) + sender.rank.ToString());
					string commands = "You can use: " + ChatCommandHandling.GetCommandList(sender.rank);
					ChatCommandHandling.WrapMessage(sender, commands);
				}
				else
				{
					if (args[0] == '/') args = args.Substring(1);
					string help = ChatCommandHandling.GetHelp(args);
					if (help == "") {
						sender.PrintMessage(Color.CommandError + "No help text on /" + args);
					} else {
						ChatCommandHandling.WrapMessage(sender, help);
					}
				}
			}
		}

		public class ThirdPerson : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Guest; }
			}

			public override string HelpMsg
			{
				get { return "Third-person roleplay-like actions ( * Bob uses magic!)"; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				/* /me /me Easter egg. */
				if (args == "/me")
				{
					sender.PrintMessage(Color.CommandResult + "Red alert, /me /me found, PMing all players!");
					sender.PrintMessage(Color.CommandResult + "Easter egg get!");
				}
				else if (args == "")
				{
					sender.PrintMessage(Color.CommandError + "No /me message specified");
				}
				else
				{
					Server.theServ.MessageAll(" * " + sender.name + " " + args);
				}
			}
		}
		
		public class ClearChat : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Guest; }
			}

			public override string HelpMsg
			{
				get { return "Clear the chat log of the user"; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				for (int ww = 0; ww < 20; ++ww)
				{
					sender.PrintMessage("");
				}
			}
		}

		public class WhoIs : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Builder; }
			}

			public override string HelpMsg
			{
				get { return "Get information on a user."; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				args = args.Trim();
				Player p = Server.theServ.GetPlayer(args);
				Rank r = Player.RankOf(args);
				if(p == null) {
					sender.PrintMessage(Color.CommandResult + args + " is offline");
					sender.PrintMessage(Color.CommandResult + args + " is a " + RankInfo.RankColor(r) + r.ToString());
				} else {
					args = p.name;
					sender.PrintMessage(Color.CommandResult + args + " is online");
					sender.PrintMessage(Color.CommandResult + args + " is a " + RankInfo.RankColor(r) + r.ToString());
					sender.PrintMessage(Color.CommandResult + args + " is at: " + p.pos.x/32 + "," + p.pos.y/32 + "," + p.pos.z/32);
					sender.PrintMessage(Color.CommandResult + args + " is player #" + p.playerID);
				}
			}
		}

		public class Rules : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Guest; }
			}

			public override string HelpMsg
			{
				get { return "Show the server rules."; }
			}

			public override void Run(Player sender, string cmd, string arg)
			{
				ChatCommandHandling.WrapMessage(sender, ChatCommandHandling.RulesText);
			}
		}
	}
}