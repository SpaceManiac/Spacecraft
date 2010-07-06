using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace spacecraft
{
	public class ChatCommandHandling
	{
		public static string RulesText;
		static Dictionary<String, ChatCommands.ChatCommandBase> Commands;

		static ChatCommandHandling()
		{
			Commands = new Dictionary<String, ChatCommands.ChatCommandBase>();
			
			Commands.Add("clear", new ChatCommands.ClearChat());
			Commands.Add("go", new ChatCommands.LandmarkGoto());
			Commands.Add("help", new ChatCommands.Help());
			Commands.Add("me", new ChatCommands.ThirdPerson());
			Commands.Add("staff", new ChatCommands.StaffList());
			
			Commands.Add("mark", new ChatCommands.LandmarkAdd());
			Commands.Add("paint", new ChatCommands.Paint());
			Commands.Add("teleport", new ChatCommands.Teleport());
			Commands.Add("tp", new ChatCommands.Teleport());
			Commands.Add("whois", new ChatCommands.WhoIs());
			
			Commands.Add("ban", new ChatCommands.RankBanned());
			Commands.Add("bring", new ChatCommands.Bring());
			Commands.Add("broadcast", new ChatCommands.Broadcast());
			Commands.Add("builder", new ChatCommands.RankBuilder());
			Commands.Add("conwiz", new ChatCommands.ConspiracyWizard());
			Commands.Add("dehydrate", new ChatCommands.Dehydrate());
			Commands.Add("diagnostics", new ChatCommands.Diagnostics());
			Commands.Add("diag", new ChatCommands.Diagnostics());
			Commands.Add("guest", new ChatCommands.RankGuest());
			Commands.Add("kick", new ChatCommands.Kick());
			Commands.Add("mob", new ChatCommands.SpawnMob());
			Commands.Add("physics", new ChatCommands.Physics());
			Commands.Add("place", new ChatCommands.Place());
			Commands.Add("rerank", new ChatCommands.ReloadRanks());
			Commands.Add("rmmark", new ChatCommands.LandmarkRemove());
			Commands.Add("say", new ChatCommands.Broadcast());
			Commands.Add("unban", new ChatCommands.RankGuest());
			
			Commands.Add("config", new ChatCommands.Configure());
			Commands.Add("convert", new ChatCommands.Convert());
			Commands.Add("exit", new ChatCommands.Exit());
			Commands.Add("resend", new ChatCommands.ResendMap());
			Commands.Add("save", new ChatCommands.Save());
			Commands.Add("setspawn", new ChatCommands.SetSpawn());
			Commands.Add("mod", new ChatCommands.RankMod());

			if(File.Exists("rules.txt")) {
				RulesText = File.ReadAllText("rules.txt").TrimEnd();
				if (RulesText != "")
					Commands.Add("rules", new ChatCommands.Rules());
			}
		}

		/// <summary>
		/// Lookup the command cmd, and execute it using args as the arguments. sender is used to post error messages back to the user.
		/// </summary>
		/// <param name="sender">The Player attempting to execute the command.</param>
		/// <param name="cmd">Command to execute, e.g. "me"</param>
		/// <param name="args">Argument passed to command, e.g. "uses /me sucessfully"</param>
		///
		static public void Execute(Player sender, string cmd, string args)
		{
			if (Commands.ContainsKey(cmd))
			{
				if (sender.rank >= Commands[cmd].RankNeeded) {
					try {
						Commands[cmd].Run(sender, cmd, args);
					}
					catch(NotImplementedException) {
						sender.PrintMessage(Color.CommandError + "That command's not implemented!");
					}
				} else {
					sender.PrintMessage(Color.CommandError + "You don't have permission to use that command.");
				}
			} else {
				sender.PrintMessage(Color.CommandError + "Unknown command " + cmd);
			}
		}

		static public void WrapMessage(Player sendto, string message, string prefix)
		{
			if(prefix.Length > 4)
				prefix = prefix.Substring(0, 4);
			
			while (message.Length > 60)
			{
				int i = message.LastIndexOf(' ', 60, 60);
				if(i == -1) i = 60;
				
				sendto.PrintMessage(prefix + message.Substring(0, i));
				message = message.Substring(i);
			}
			sendto.PrintMessage(prefix + message);
		}

		static public void WrapMessage(Player sendto, string message)
		{
			WrapMessage(sendto, message, Color.CommandResult);
		}

		static public string GetHelp(string cmd)
		{
			if (Commands.ContainsKey(cmd))
			{
				return Commands[cmd].HelpMsg + " (" + Commands[cmd].RankNeeded.ToString() + ")";
			}
			else
			{
				return "";
			}
		}

		static public string GetCommandList(Rank rank)
		{
			string result = "";
			foreach (KeyValuePair<string, ChatCommands.ChatCommandBase> kvp in Commands)
			{
				if (rank >= kvp.Value.RankNeeded)
				{
					result += " " + kvp.Key;
				}
			}
			return result;
		}
	}

	namespace ChatCommands
	{
		public abstract class ChatCommandBase
		{
			public abstract Rank RankNeeded { get; }
			public abstract string HelpMsg { get; }
			public abstract void Run(Player sender, string cmd, string arg);
		}

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

		public class Bring : ChatCommandBase
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
						Spacecraft.Log(sender.name + " brought " + p.name);
					}
				}
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

		public class Place : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Mod; }
			}

			public override string HelpMsg
			{
				get { return "Place certain special blocks."; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				args = args.ToLower();
				if (args == "")
				{
					if (sender.placing)
					{
						sender.placing = false;
						sender.PrintMessage(Color.CommandResult + "No longer placing");
					}
					else
					{
						sender.PrintMessage(Color.CommandError + "No block specified");
					}
				}
				else
				{
					string b = args;
					if (BlockInfo.NameExists(b))
					{
						sender.placing = true;
						sender.placeType = BlockInfo.names[b];
						sender.PrintMessage(Color.CommandResult + "Placing " + b + " in place of Obsidian. Use /place to cancel");
					}
					else
					{
						sender.PrintMessage(Color.CommandError + "Unknown block " + b);
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
				get { return Rank.Admin; }
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

		public class LandmarkGoto : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Guest; }
			}

			public override string HelpMsg
			{
				get { return "Teleport to a landmark."; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				args = args.Trim().ToLower();
				Map map = Server.theServ.map;
				if (args == "")
				{
					string marks = "Landmarks: " + String.Join(", ", map.GetLandmarkList());
					ChatCommandHandling.WrapMessage(sender, marks);
				}
				else
				{
					if (map.landmarks.ContainsKey(args))
					{
						Position p = map.landmarks[args].First;
						byte heading = map.landmarks[args].Second;
						Server.theServ.MovePlayer(sender, p, heading, 0);
						sender.PrintMessage(Color.CommandResult + "Teleported to landmark " + args);
					}
					else
					{
						sender.PrintMessage(Color.CommandError + "No such landmark " + args);
					}
				}
			}
		}

		public class LandmarkAdd : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Builder; }
			}

			public override string HelpMsg
			{
				get { return "Create a landmark."; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				args = args.Trim().ToLower();
				Map map = Server.theServ.map;
				if (args == "")
				{
					string marks = "Landmarks: " + String.Join(", ", map.GetLandmarkList());
					ChatCommandHandling.WrapMessage(sender, marks);
				}
				else
				{
					if (map.landmarks.ContainsKey(args))
					{
						sender.PrintMessage(Color.CommandError + "Landmark " + args + " already exists");
					}
					else
					{
						byte heading = sender.heading;
						map.landmarks.Add(args, new Pair<Position, byte>(sender.pos, heading));
						Server.theServ.MessageAll(Color.Announce + sender.name + " created landmark " + args);
					}
				}
			}
		}

		public class LandmarkRemove : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Mod; }
			}

			public override string HelpMsg
			{
				get { return "Remove a landmark."; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				args = args.Trim().ToLower();
				Map map = Server.theServ.map;
				if (args == "")
				{
					string marks = "Landmarks: " + String.Join(", ", map.GetLandmarkList());
					ChatCommandHandling.WrapMessage(sender, marks);
				}
				else
				{
					if (map.landmarks.ContainsKey(args))
					{
						map.landmarks.Remove(args);
						Server.theServ.MessageAll(Color.Announce + sender.name + " removed landmark " + args);
					}
					else
					{
						sender.PrintMessage(Color.CommandError + "No such landmark " + args);
					}
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

		public class ConspiracyWizard : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Mod; }
			}

			public override string HelpMsg
			{
				get { return "What does /conwiz do? What DOESN'T it do?"; }
			}

			public override void Run(Player sender, string cmd, string arg)
			{
				Position oldpos = sender.pos;
				Position pos = new Position((short)(oldpos.x / 32), (short)(oldpos.y / 32), (short)(oldpos.z / 32));

				Map map = Server.theServ.map;

				int value = Spacecraft.random.Next(21, 36);

				for (short x = 0; x < map.xdim; x++)
				{
					for (short y = 0; y < map.ydim; y++)
					{
						for (short z = 0; z < map.zdim; z++)
						{
							if (Math.Abs(pos.x - x) + Math.Abs(pos.y - y) + Math.Abs(pos.z - z) == 5)
							{
								Server.theServ.ChangeBlock(new BlockPosition(x, y, z),(Block) value);
							}
							if (Math.Abs(pos.x - x) + Math.Abs(pos.y - y) + Math.Abs(pos.z - z) < 5)
							{
								Server.theServ.ChangeBlock(new BlockPosition(x, y, z), Block.Air);
							}
						}
					}
				}
				Spacecraft.Log(sender.name + " helped with conspiracies");
			}
		}

		public class Convert : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Admin; }
			}

			public override string HelpMsg
			{
				get { return "Replace one block with another within a radius (/convert original new)"; }
			}

			public override void Run(Player sender, string cmd, string args)
			{
				args = args.ToLower();

				string[] parts = args.Split(new char[]{' '});
				if (parts.Length < 2)
				{
					sender.PrintMessage(Color.CommandError + "Too few arguments!");
					return;
				}

				if(!BlockInfo.NameExists(parts[0])) {
					sender.PrintMessage(Color.CommandError + "No such block " + parts[0]);
					return;
				} else if(!BlockInfo.NameExists(parts[1])) {
					sender.PrintMessage(Color.CommandError + "No such block " + parts[1]);
					return;
				}

				Block From = BlockInfo.names[parts[0]];
				Block To = BlockInfo.names[parts[1]];
				Map map = Server.theServ.map;
				BlockPosition pos = new BlockPosition((short)(sender.pos.x / 32), (short)(sender.pos.y / 32), (short)(sender.pos.z / 32));

				int i = 0;
				for (short x = 0; x < map.xdim; x++) {
					for (short y = 0; y < map.ydim; y++) {
						for (short z = 0; z < map.zdim; z++) {
							if (map.GetTile(x, y, z) == From && (Math.Abs(pos.x - x) + Math.Abs(pos.y - y) + Math.Abs(pos.z - z) < 20))
							{
								++i;
								map.SetTile(x, y, z, To);
							}
						}
					}
				}

				sender.PrintMessage("Converted " + i.ToString() + " " + From.ToString() + " to " + To.ToString());
				Spacecraft.Log(sender.name + " converted " + From.ToString() + " to " + To.ToString());
			}
		}
		public class Save : ChatCommandBase
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

		public class Paint : ChatCommandBase
		{
			public override Rank RankNeeded
			{
				get { return Rank.Builder; }
			}

			public override string HelpMsg
			{
				get { return "Toggle painting mode. In painting mode, removes turn into places."; }
			}

			public override void Run(Player sender, string cmd, string arg)
			{
				sender.painting = !sender.painting;
				sender.PrintMessage(Color.CommandResult + "Paint mode: " + sender.painting.ToString());
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
				
				double cpu = Spacecraft.cpuCounter.NextValue();
				cpu = Math.Round(10 * cpu) / 10.0;
				
				double ram = p.PagedMemorySize64 / (1024.0 * 1024.0);
				ram = Math.Round(10 * ram) / 10.0;
				
                sender.PrintMessage(Color.CommandResult + "Spacecraft @ " + s.IP + ":" + s.port + " (http: " + s.HTTPport + ")");
				sender.PrintMessage(Color.CommandResult + "Players online: " + s.Players.Count);
				sender.PrintMessage(Color.CommandResult + "ActiveList length: " + s.map.ActiveListLength + " - Updates last tick: " + s.map.UpdatedLastTick);
				sender.PrintMessage(Color.CommandResult + "Server uptime: " + Uptime);
				sender.PrintMessage(Color.CommandResult + "Last heartbeat took: " + s.LastHeartbeatTook + "s - Last physics tick took: " + s.LastPhysicsTickTook + "s");
				sender.PrintMessage(Color.CommandResult + "CPU usage: " + cpu + "% - RAM usage: " + ram + "Mb");
			}
		}
	}
}