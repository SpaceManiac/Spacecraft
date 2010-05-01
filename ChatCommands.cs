using System;
using System.Text;
using System.Collections.Generic;

public class ChatCommandHandling
{
    static Dictionary<String, ChatCommands.ChatCommandBase> Commands;

    static ChatCommandHandling()
    {
        Commands = new Dictionary<String, ChatCommands.ChatCommandBase>();

        Commands.Add("help", new ChatCommands.Help());
        Commands.Add("me", new ChatCommands.ThirdPerson());
		Commands.Add("bring", new ChatCommands.Bring());
		Commands.Add("exit", new ChatCommands.Exit());
		Commands.Add("setspawn", new ChatCommands.SetSpawn());
		Commands.Add("place", new ChatCommands.Place());
		Commands.Add("teleport", new ChatCommands.Teleport());
		Commands.Add("tp", new ChatCommands.Teleport());
		Commands.Add("kick", new ChatCommands.Kick());
		Commands.Add("k", new ChatCommands.Kick());
		Commands.Add("broadcast", new ChatCommands.Broadcast());
		Commands.Add("say", new ChatCommands.Broadcast());
		Commands.Add("dehydrate", new ChatCommands.Dehydrate());
		Commands.Add("mob", new ChatCommands.SpawnMob());
		Commands.Add("resend", new ChatCommands.ResendMap());
		Commands.Add("rerank", new ChatCommands.ReloadRanks());
		Commands.Add("clear", new ChatCommands.ClearChat());
    }

    /// <summary>
    /// Lookup the command cmd, and execute it using args as the arguments. sender is used to post error messages back to the user.
    /// </summary>
    /// <param name="sender">The Connection attempting to execute the command.</param>
    /// <param name="cmd">Command to execute, e.g. "me"</param>
    /// <param name="args">Argument passed to command, e.g. "uses /me sucessfully"</param>
    /// 
    static public void Execute(Connection sender, string cmd, string args)
    {
        if (Commands.ContainsKey(cmd)) {
            if (sender.player.rank >= Commands[cmd].RankNeeded) {
                Commands[cmd].Run(sender, cmd, args);
			} else {
                sender.Message(Color.DarkRed + "You don't have permission to do that.");
			}
        } else {
            sender.Message(Color.DarkRed + "Unknown command " + cmd);
		}
    }

    static public string GetHelp(string cmd)
	{
		if (Commands.ContainsKey(cmd)) {
			return Commands[cmd].HelpMsg;
		} else {
			return "";
		}
	}

    static public string GetCommandList(Rank rank)
	{
		string result = "";
		foreach(KeyValuePair<string, ChatCommands.ChatCommandBase> kvp in Commands) {
			if(rank >= kvp.Value.RankNeeded) {
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
        public abstract void Run(Connection sender, string cmd, string arg);
    }

    public class Help : ChatCommandBase
    {
        public override Rank RankNeeded
        {
            get { return Rank.Banned; }
        }

        public override string HelpMsg
        {
            get { return "/help: displays help information"; }
        }

        public override void Run(Connection sender, string cmd, string args)
        {
            if (args == "") {
                sender.Message(Color.Teal + "You are a " + Player.RankColor(sender.player.rank) + sender.player.rank.ToString());
                string commands = "You can use:" + ChatCommandHandling.GetCommandList(sender._player.rank);
				if(commands.Length <= 60) {
                	sender.Message(Color.Teal + commands);
				} else {
					while(commands.Length > 60) {
						int i = commands.LastIndexOf(' ', 60, 60);
						sender.Message(Color.Teal + " " + commands.Substring(0, i));
						commands = commands.Substring(i + 1);
					}
					sender.Message(Color.Teal + " " + commands);
				}
            } else {
                if (args[0] == '/') args = args.Substring(1);
                string help = ChatCommandHandling.GetHelp(args);
                if (help == "") {
                    sender.Message(Color.DarkRed + "No help text on /" + args);
                } else {
                    sender.Message(Color.Teal + help);
                }
            }
        }
    }

    public class ThirdPerson : ChatCommandBase
    {
        public override Rank RankNeeded {
            get { return Rank.Banned; }
        }

        public override string HelpMsg {
            get { return "/me: third-person roleplay-like actions"; }
        }

        public override void Run(Connection sender, string cmd, string args)
        {
            /* /me /me Easter egg. */
            if (args == "/me") {
                sender.Message(Color.Teal + "Red alert, /me /me found, PMing all players!");
                sender.Message(Color.Teal + "Easter egg get!");
            } else if (args == "") {
                sender.Message(Color.DarkRed + "No /me message specified");
            } else {
                Connection.MsgAll(" * " + sender.player.name + " " + args);
            }
        }
    }

    public class Bring : ChatCommandBase
    {
        public override Rank RankNeeded {
            get { return Rank.Mod; }
        }

        public override string HelpMsg {
            get { return "/bring: teleports a player to you (mod+)"; }
        }

        public override void Run(Connection sender, string cmd, string args)
        {
            if (args == "") {
                sender.Message(Color.DarkRed + "No player specified");
            } else {
                Connection c = Server.theServ.GetConnection(args);
                if (c == null) {
                    sender.Message(Color.DarkRed + "No such player " + args);
                } else {
                    Player _player = sender.player;
                    c.Send(Connection.PacketTeleportSelf(_player.x, _player.y, _player.z, _player.heading, _player.pitch));
                }
            }
        }
    }
	
	public class Exit : ChatCommandBase
	{
		public override Rank RankNeeded {
			get { return Rank.Admin; }
		}
		
		public override string HelpMsg {
			get { return "/exit: shuts down the server (admin)"; }
		}
		
		public override void Run(Connection sender, string cmd, string args)
		{
            Server.theServ.SendAll(Connection.PacketKick("Server is shutting down!"));
            Server.OnExit.Set();
		}
	}
	
	public class SetSpawn : ChatCommandBase
	{
		public override Rank RankNeeded {
			get { return Rank.Admin; }
		}
		
		public override string HelpMsg {
			get { return "/setspawn: sets the global spawn point (admin)"; }
		}
		
		public override void Run(Connection sender, string cmd, string args)
		{
			Server.theServ.map.xspawn = sender._player.x;
			Server.theServ.map.yspawn = sender._player.y;
			Server.theServ.map.zspawn = sender._player.z;
			sender.Message(Color.Teal + "Spawn point set");
		}
	}
	
	public class Place : ChatCommandBase
	{
		public override Rank RankNeeded {
			get { return Rank.Mod; }
		}
		
		public override string HelpMsg {
			get { return "/place: place special blocks (mod+)"; }
		}
		
		public override void Run(Connection sender, string cmd, string args)
		{
            if(args == "") {
                if(sender._player.placing) {
                    sender._player.placing = false;
                    sender.Message(Color.Teal + "No longer placing");
                } else {
                    sender.Message(Color.DarkRed + "No block specified");
                }
            } else {
                string b = args;
                if(Block.Names.Contains(b)) {
                    sender._player.placing = true;
                    sender._player.placeType = (byte)(Block.Names[b]);
                    sender.Message(Color.Teal + "Placing " + b + " in place of Obsidian. Use /place to cancel");
                } else {
                    sender.Message(Color.DarkRed + "Unknown block " + b);
                }
            }
		}
	}
	
	public class Teleport : ChatCommandBase
	{
		public override Rank RankNeeded {
			get { return Rank.Builder; }
		}
		
		public override string HelpMsg {
			get { return "/teleport, /tp: teleport to a player (builder+)"; }
		}
		
		public override void Run(Connection sender, string cmd, string args)
		{
            if(args == "") {
                sender.Message(Color.DarkRed + "No player specified");
            } else {
                string pname = args;
                Player p = Server.theServ.GetPlayer(pname);
                if(p == null) {
                    sender.Message(Color.DarkRed + "No such player " + pname);
                } else {
                    sender.Send(Connection.PacketTeleportSelf(p.x, p.y, p.z, p.heading, p.pitch));
                }
            }
		}
	}
	
	public class Kick : ChatCommandBase
	{
		public override Rank RankNeeded {
			get { return Rank.Mod; }
		}
		
		public override string HelpMsg {
			get { return "/kick, /k: kick a player (mod+)"; }
		}
		
		public override void Run(Connection sender, string cmd, string args)
		{
            if(args == "") {
                sender.Message(Color.DarkRed + "No player specified");
            } else {
                string pname = args;
                Connection c = Server.theServ.GetConnection(pname);
                if(c == null) {
                    sender.Message(Color.DarkRed + "No such player " + pname);
                } else {
                    c.Kick("You were kicked by " + sender.name);
                }
            }
		}
	}
	
	public class Broadcast : ChatCommandBase
	{
		public override Rank RankNeeded {
			get { return Rank.Mod; }
		}
		
		public override string HelpMsg {
			get { return "/broadcast, /say: broadcast a message in yellow text (mod+)"; }
		}
		
		public override void Run(Connection sender, string cmd, string args)
		{
            if(args == "") {
                sender.Message(Color.DarkRed + "No message specified");
            } else {
                Connection.MsgAll(Color.Yellow + args);
            }
		}
	}
	
	public class Dehydrate : ChatCommandBase
	{
		public override Rank RankNeeded {
			get { return Rank.Mod; }
		}
		
		public override string HelpMsg {
			get { return "/dehydrate: remove all liquids in case of flood (mod+)"; }
		}
		
		public override void Run(Connection sender, string cmd, string args)
		{
            Server.theServ.map.Dehydrate(Server.theServ);
		}
	}
	
	public class SpawnMob : ChatCommandBase
	{
		public override Rank RankNeeded {
			get { return Rank.Mod; }
		}
		
		public override string HelpMsg {
			get { return "/mob: spawn an AI mob (mod+)"; }
		}
		
		public override void Run(Connection sender, string cmd, string args)
		{
			Server.theServ.SpawnMob(sender._player, args);
		}
	}
	
	public class ResendMap : ChatCommandBase
	{
		public override Rank RankNeeded {
			get { return Rank.Guest; }
		}
		
		public override string HelpMsg {
			get { return "/resend: resend the map for testing purposes (brokenish)"; }
		}
		
		public override void Run(Connection sender, string cmd, string args)
		{
			sender.ResendMap();
		}
	}
	
	public class ReloadRanks : ChatCommandBase
	{
		public override Rank RankNeeded {
			get { return Rank.Mod; }
		}
		
		public override string HelpMsg {
			get { return "/rerank: reload ranks from the rank (mod+)"; }
		}
		
		public override void Run(Connection sender, string cmd, string args)
		{
			Spacecraft.LoadRanks();
			sender.Message(Color.Teal + "Ranks reloaded");
		}
	}
	
	public class ClearChat : ChatCommandBase
	{
		public override Rank RankNeeded {
			get { return Rank.Guest; }
		}
		
		public override string HelpMsg {
			get { return "/clear: clears the chat log"; }
		}
		
		public override void Run(Connection sender, string cmd, string args)
		{
			for(int ww = 0; ww < 20; ++ww) {
				sender.Message("");
			}
		}
	}	
}