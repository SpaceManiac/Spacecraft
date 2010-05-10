using System;
using System.Text;
using System.Collections.Generic;

namespace spacecraft
{
    public class ChatCommandHandling
    {
        static Dictionary<String, ChatCommands.ChatCommandBase> Commands;

        static ChatCommandHandling()
        {
            Commands = new Dictionary<String, ChatCommands.ChatCommandBase>();
			
            Commands.Add("me", new ChatCommands.ThirdPerson());
            Commands.Add("help", new ChatCommands.Help());
            Commands.Add("teleport", new ChatCommands.Teleport());
            Commands.Add("tp", new ChatCommands.Teleport());
            Commands.Add("bring", new ChatCommands.Bring());
            Commands.Add("exit", new ChatCommands.Exit());
            Commands.Add("setspawn", new ChatCommands.SetSpawn());
            Commands.Add("place", new ChatCommands.Place());
            Commands.Add("kick", new ChatCommands.Kick());
            Commands.Add("k", new ChatCommands.Kick());
            Commands.Add("broadcast", new ChatCommands.Broadcast());
            Commands.Add("say", new ChatCommands.Broadcast());
            Commands.Add("dehydrate", new ChatCommands.Dehydrate());
            Commands.Add("mob", new ChatCommands.SpawnMob());
            Commands.Add("resend", new ChatCommands.ResendMap());
            Commands.Add("rerank", new ChatCommands.ReloadRanks());
            Commands.Add("clear", new ChatCommands.ClearChat());
            Commands.Add("go", new ChatCommands.LandmarkGoto());
            Commands.Add("mark", new ChatCommands.LandmarkAdd());
            Commands.Add("rmmark", new ChatCommands.LandmarkRemove());
            Commands.Add("config", new ChatCommands.Configure());
			Commands.Add("whois", new ChatCommands.WhoIs());
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
            if (Commands.ContainsKey(cmd))
            {
                if (sender.player.rank >= Commands[cmd].RankNeeded)
                {
                    Commands[cmd].Run(sender, cmd, args);
                }
                else
                {
                    sender.Message(Color.DarkRed + "You don't have permission to do that.");
                }
            }
            else
            {
                sender.Message(Color.DarkRed + "Unknown command " + cmd);
            }
        }

        static public void WrapMessage(Connection sendto, string message)
        {
            while (message.Length > 60)
            {
                int i = message.LastIndexOf(' ', 60, 60);
                sendto.Message(Color.Teal + message.Substring(0, i));
                message = message.Substring(i);
            }
            sendto.Message(Color.Teal + message);
        }

        static public string GetHelp(string cmd)
        {
            if (Commands.ContainsKey(cmd))
            {
                return Commands[cmd].HelpMsg;
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
                if (args == "")
                {
                    sender.Message(Color.Teal + "You are a " + Player.RankColor(sender.player.rank) + sender.player.rank.ToString());
                    string commands = "You can use:" + ChatCommandHandling.GetCommandList(sender._player.rank);
                    ChatCommandHandling.WrapMessage(sender, commands);
                }
                else
                {
                    if (args[0] == '/') args = args.Substring(1);
                    string help = ChatCommandHandling.GetHelp(args);
                    if (help == "")
                    {
                        sender.Message(Color.DarkRed + "No help text on /" + args);
                    }
                    else
                    {
                        sender.Message(Color.Teal + help);
                    }
                }
            }
        }

        public class ThirdPerson : ChatCommandBase
        {
            public override Rank RankNeeded
            {
                get { return Rank.Banned; }
            }

            public override string HelpMsg
            {
                get { return "/me: third-person roleplay-like actions"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                /* /me /me Easter egg. */
                if (args == "/me")
                {
                    sender.Message(Color.Teal + "Red alert, /me /me found, PMing all players!");
                    sender.Message(Color.Teal + "Easter egg get!");
                }
                else if (args == "")
                {
                    sender.Message(Color.DarkRed + "No /me message specified");
                }
                else
                {
                    Connection.MsgAll(" * " + sender.name + " " + args);
                    Spacecraft.Log("* " + sender.name + " " + args);
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
                get { return "/bring: teleports a player to you (mod+)"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                if (args == "")
                {
                    sender.Message(Color.DarkRed + "No player specified");
                }
                else
                {
                    Connection c = MinecraftServer.theServ.GetConnection(args);
                    if (c == null)
                    {
                        sender.Message(Color.DarkRed + "No such player " + args);
                    }
                    else
                    {
                        Player _player = sender.player;
                        c.Send(Connection.PacketTeleportSelf(_player.x, _player.y, _player.z, _player.heading, _player.pitch));
                        Spacecraft.Log(sender.name + " brought " + args);
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
                get { return "/exit: shuts down the server (admin)"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                Spacecraft.Log(sender.name + " shut down the server");
                MinecraftServer.theServ.SendAll(Connection.PacketKick("Server is shutting down!"));
                MinecraftServer.OnExit.Set();
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
                get { return "/setspawn: sets the global spawn point (admin)"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                MinecraftServer.theServ.map.SetSpawn(new Position(
                    sender._player.x,
                    sender._player.y,
                    sender._player.z
                ), sender._player.heading);
                sender.Message(Color.Teal + "Spawn point set");
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
                get { return "/place: place special blocks (mod+)"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                if (args == "")
                {
                    if (sender._player.placing)
                    {
                        sender._player.placing = false;
                        sender.Message(Color.Teal + "No longer placing");
                    }
                    else
                    {
                        sender.Message(Color.DarkRed + "No block specified");
                    }
                }
                else
                {
                    string b = args;
                    if (BlockInfo.NameExists(b))
                    {
                        sender._player.placing = true;
                        sender._player.placeType = BlockInfo.names[b];
                        sender.Message(Color.Teal + "Placing " + b + " in place of Obsidian. Use /place to cancel");
                    }
                    else
                    {
                        sender.Message(Color.DarkRed + "Unknown block " + b);
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
                get { return "/teleport, /tp: teleport to a player (builder+)"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                if (args == "")
                {
                    sender.Message(Color.DarkRed + "No player specified");
                }
                else
                {
                    string pname = args;
                    Player p = MinecraftServer.theServ.GetPlayer(pname);
                    if (p == null)
                    {
                        sender.Message(Color.DarkRed + "No such player " + pname);
                    }
                    else
                    {
                        sender.Send(Connection.PacketTeleportSelf(p.x, p.y, p.z, p.heading, p.pitch));
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
                get { return "/kick, /k: kick a player (mod+)"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                if (args == "")
                {
                    sender.Message(Color.DarkRed + "No player specified");
                }
                else
                {
                    string pname = args;
                    Connection c = MinecraftServer.theServ.GetConnection(pname);
                    if (c == null)
                    {
                        sender.Message(Color.DarkRed + "No such player " + pname);
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
                get { return "/broadcast, /say: broadcast a message in yellow text (mod+)"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                if (args == "")
                {
                    sender.Message(Color.DarkRed + "No message specified");
                }
                else
                {
                    Connection.MsgAll(Color.Yellow + args);
                    Spacecraft.Log("{" + sender.name + "} " + args);
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
                get { return "/dehydrate: remove all liquids in case of flood (mod+)"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                MinecraftServer.theServ.map.Dehydrate(MinecraftServer.theServ);
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
                get { return "/mob: spawn an AI mob (mod+)"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                MinecraftServer.theServ.SpawnMob(sender._player, args);
            }
        }

        public class ResendMap : ChatCommandBase
        {
            public override Rank RankNeeded
            {
                get { return Rank.Guest; }
            }

            public override string HelpMsg
            {
                get { return "/resend: resend the map for testing purposes (brokenish)"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                sender.ResendMap();
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
                get { return "/rerank: reload ranks from the rank (mod+)"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                Spacecraft.LoadRanks();
                Spacecraft.Log(sender.name + " reloaded the ranks");
                sender.Message(Color.Teal + "Ranks reloaded");
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
                get { return "/clear: clears the chat log"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                for (int ww = 0; ww < 20; ++ww)
                {
                    sender.Message("");
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
                get { return "/config: manages raw server options"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                string[] argv = args.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (argv.Length == 0)
                {
                    string list = "Options set:" + Config.GetDefinedList();
                    ChatCommandHandling.WrapMessage(sender, list);
                }
                else if (argv.Length == 1)
                {
                    string r = Config.Get(argv[0], null);
                    if (r == null)
                    {
                        sender.Message(Color.Teal + "No option " + argv[0] + " defined");
                    }
                    else
                    {
                        sender.Message(Color.Teal + "Option " + argv[0] + " is " + r);
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
                get { return "/go: teleports you to a landmark"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
				args = args.Trim().ToLower();
                Map map = MinecraftServer.theServ.map;
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
                        sender.Send(Connection.PacketTeleportSelf(p.x, p.y, p.z, heading, 0));
                        sender.Message(Color.Teal + "Teleported to landmark " + args);
                    }
                    else
                    {
                        sender.Message(Color.DarkRed + "No such landmark " + args);
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
                get { return "/mark: creates a landmark"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
				args = args.Trim().ToLower();
                Map map = MinecraftServer.theServ.map;
                if (args == "")
                {
                    string marks = "Landmarks: " + String.Join(", ", map.GetLandmarkList());
                    ChatCommandHandling.WrapMessage(sender, marks);
                }
                else
                {
                    if (map.landmarks.ContainsKey(args))
                    {
                        sender.Message(Color.DarkRed + "Landmark " + args + " already exists");
                    }
                    else
                    {
                        Position p = new Position(sender._player.x, sender._player.y, sender._player.z);
                        byte heading = sender._player.heading;
                        map.landmarks.Add(args, new Pair<Position, byte>(p, heading));
                        Connection.MsgAll(Color.Yellow + "Landmark " + args + " created");
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
                get { return "/rmmark: removes a landmark"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
				args = args.Trim().ToLower();
                Map map = MinecraftServer.theServ.map;
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
						Connection.MsgAll(Color.Yellow + "Landmark " + args + " removed");
                    }
                    else
                    {
                        sender.Message(Color.DarkRed + "No such landmark " + args);
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
                get { return "/whois: get information on a user"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
				args = args.Trim();
                Player p = MinecraftServer.theServ.GetPlayer(args);
				Rank r = Player.LookupRank(args);
				if(p == null) {
					sender.Message(Color.Teal + args + " is offline");
					sender.Message(Color.Teal + args + " is a " + Player.RankColor(r) + r.ToString());
				} else {
					sender.Message(Color.Teal + args + " is online");
					sender.Message(Color.Teal + args + " is a " + Player.RankColor(r) + r.ToString());
					sender.Message(Color.Teal + args + " is at: " + p.x + "," + p.y + "," + p.z);
				}
            }
        }
    }
}