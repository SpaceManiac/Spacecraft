using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace spacecraft {
	namespace ChatCommands {
		public class ReloadRanks : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Mod; }
			}

			public override string HelpMsg
			{
				get { return "Refresh the rank database"; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				Player.LoadRanks();
				Spacecraft.Log(sender.name + " reloaded the ranks");
				sender.PrintMessage(Color.CommandResult + "Ranks reloaded");
			}
		}
		
		public class RankBanned : ChatCommandBase
		{
			public override Rank RankNeeded {
				get { return Rank.Mod; }
			}
			
			public override string HelpMsg {
				get { return "Ban a user of lesser rank."; }
			}
			
			public override void Run(Player sender, string cmd, string arg)
			{
				if(arg == "") {
					sender.PrintMessage(Color.CommandError + "No player specified");
				} else {
					string name = arg.Trim();
					Player P = Server.theServ.GetPlayer(arg);
					if(P != null) {
						name = P.name;
					}
					
					Rank current = Player.RankOf(name);
					if(current >= sender.rank) {
						sender.PrintMessage(Color.CommandError + "You can't change the rank of someone of an equal or greater rank!");
						return;
					}
					
					if(P == null) {
						// just set their rank
						Player.SetRankOf(name, Rank.Banned);
					} else {
						// they're online, so we inform them
						// this calls SetRankOf as well
						P.UpdateRank(Rank.Banned);
						
						// and of course they're now banned
						P.Kick("You were banned by " + sender.name);
					}
					
					sender.PrintMessage(Color.CommandResult + name + " banned");
					Spacecraft.Log(sender.name + " banned " + name);
					
					Player.SaveRanks();
				}
			}
		}
		

		public class RankGuest : ChatCommandBase
		{
			public override Rank RankNeeded {
				get { return Rank.Mod; }
			}
			
			public override string HelpMsg {
				get { return "Set a user of lesser rank to Guest."; }
			}
			
			public override void Run(Player sender, string cmd, string arg)
			{
				if(arg == "") {
					sender.PrintMessage(Color.CommandError + "No player specified");
				} else {
					string name = arg.Trim();
					Player P = Server.theServ.GetPlayer(arg);
					if(P != null) {
						name = P.name;
					}
					
					Rank current = Player.RankOf(name);
					if(current >= sender.rank) {
						sender.PrintMessage(Color.CommandError + "You can't change the rank of someone of an equal or greater rank!");
						return;
					}
					
					if(P == null) {
						// just set their rank
						Player.SetRankOf(name, Rank.Guest);
					} else {
						// they're online, so we inform them
						// this calls SetRankOf as well
						P.UpdateRank(Rank.Guest);
					}
					
					sender.PrintMessage(Color.CommandResult + name + " set to rank Guest");
					Spacecraft.Log(sender.name + " set " + name + " to rank Guest");
					
					Player.SaveRanks();
				}
			}
		}
		
		public class RankBuilder : ChatCommandBase
		{
			public override Rank RankNeeded {
				get { return Rank.Mod; }
			}
			
			public override string HelpMsg {
				get { return "Set a user of lesser rank to Builder."; }
			}
			
			public override void Run(Player sender, string cmd, string arg)
			{
				if(arg == "") {
					sender.PrintMessage(Color.CommandError + "No player specified");
				} else {
					string name = arg.Trim();
					Player P = Server.theServ.GetPlayer(arg);
					if(P != null) {
						name = P.name;
					}
					
					Rank current = Player.RankOf(name);
					if(current >= sender.rank) {
						sender.PrintMessage(Color.CommandError + "You can't change the rank of someone of an equal or greater rank!");
						return;
					}
					
					if(P == null) {
						// just set their rank
						Player.SetRankOf(name, Rank.Builder);
					} else {
						// they're online, so we inform them
						// this calls SetRankOf as well
						P.UpdateRank(Rank.Builder);
					}
					
					sender.PrintMessage(Color.CommandResult + name + " set to rank Builder");
					Spacecraft.Log(sender.name + " set " + name + " to rank Builder");
					
					Player.SaveRanks();
				}
			}
		}
		
		public class RankMod : ChatCommandBase
		{
			public override Rank RankNeeded {
				get { return Rank.Admin; }
			}
			
			public override string HelpMsg {
				get { return "Set a user of lesser rank to Mod."; }
			}
			
			public override void Run(Player sender, string cmd, string arg)
			{
				if(arg == "") {
					sender.PrintMessage(Color.CommandError + "No player specified");
				} else {
					string name = arg.Trim();
					Player P = Server.theServ.GetPlayer(arg);
					if(P != null) {
						name = P.name;
					}
					
					Rank current = Player.RankOf(name);
					if(current >= sender.rank) {
						sender.PrintMessage(Color.CommandError + "You can't change the rank of someone of an equal or greater rank!");
						return;
					}
					
					if(P == null) {
						// just set their rank
						Player.SetRankOf(name, Rank.Mod);
					} else {
						// they're online, so we inform them
						// this calls SetRankOf as well
						P.UpdateRank(Rank.Mod);
					}
					
					sender.PrintMessage(Color.CommandResult + name + " set to rank Mod");
					Spacecraft.Log(sender.name + " set " + name + " to rank Mod");
					
					Player.SaveRanks();
				}
			}
		}
		
		public class StaffList : ChatCommandBase
		{
			public override Rank RankNeeded {
				get { return Rank.Guest; }
			}
			
			public override string HelpMsg {
				get { return "Displays a list of all the server's staff."; }
			}
			
			public override void Run(Player sender, string cmd, string arg)
			{
				List<string> mods = new List<string>();
				List<string> admins = new List<string>();
				
				foreach(KeyValuePair<string, Rank> kvp in Player.PlayerRanks) {
					if(kvp.Value == Rank.Mod) {
						mods.Add(kvp.Key);
					} else if(kvp.Value == Rank.Admin) {
						admins.Add(kvp.Key);
					}
				}
				
				ChatCommandHandling.WrapMessage(sender, "Moderators: " + String.Join(", ", mods.ToArray()));
				ChatCommandHandling.WrapMessage(sender, "Administrators: " + String.Join(", ", admins.ToArray()));
			}
		}
	}
}