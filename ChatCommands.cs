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
            Commands.Add("conwiz", new ChatCommands.ConspiracyWizard());
            Commands.Add("physics", new ChatCommands.Physics());
            Commands.Add("convert", new ChatCommands.Convert());
        }

        /// <summary>
        /// Lookup the command cmd, and execute it using args as the arguments. sender is used to post error messages back to the user.
        /// </summary>
        /// <param name="sender">The NewPlayer attempting to execute the command.</param>
        /// <param name="cmd">Command to execute, e.g. "me"</param>
        /// <param name="args">Argument passed to command, e.g. "uses /me sucessfully"</param>
        /// 
        static public void Execute(NewPlayer sender, string cmd, string args)
        {
            if (Commands.ContainsKey(cmd))
            {
                if (sender.rank >= Commands[cmd].RankNeeded) {
                	try {
                    	Commands[cmd].Run(sender, cmd, args);
                    }
                    catch(NotImplementedException) {
                    	sender.PrintMessage(Color.DarkRed + "That command's not implemented!");
                    }
                } else {
                    sender.PrintMessage(Color.DarkRed + "You don't have permission to do that.");
                }
            } else {
                sender.PrintMessage(Color.DarkRed + "Unknown command " + cmd);
            }
        }

        static public void WrapMessage(NewPlayer sendto, string message)
        {
            while (message.Length > 60)
            {
                int i = message.LastIndexOf(' ', 60, 60);
                sendto.PrintMessage(Color.Teal + message.Substring(0, i));
                message = message.Substring(i);
            }
            sendto.PrintMessage(Color.Teal + message);
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
            public abstract void Run(NewPlayer sender, string cmd, string arg);
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

            public override void Run(NewPlayer sender, string cmd, string args)
            {
                if (args == "")
                {
                    sender.PrintMessage(Color.Teal + "You are a " + Player.RankColor(sender.rank) + sender.rank.ToString());
                    string commands = "You can use:" + ChatCommandHandling.GetCommandList(sender.rank);
                    ChatCommandHandling.WrapMessage(sender, commands);
                }
                else
                {
                    if (args[0] == '/') args = args.Substring(1);
                    string help = ChatCommandHandling.GetHelp(args);
                    if (help == "")
                    {
                        sender.PrintMessage(Color.DarkRed + "No help text on /" + args);
                    }
                    else
                    {
                        sender.PrintMessage(Color.Teal + help);
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

            public override void Run(NewPlayer sender, string cmd, string args)
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
                    NewServer.theServ.MessageAll(" * " + sender.name + " " + args);
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

            public override void Run(NewPlayer sender, string cmd, string args)
            {
                if (args == "")
                {
                    sender.PrintMessage(Color.DarkRed + "No player specified");
                }
                else
                {
                    NewPlayer p = NewServer.theServ.GetPlayer(args);
                    if (p == null) {
                        sender.PrintMessage(Color.DarkRed + "No such player " + args);
                    } else {
                        NewServer.theServ.MovePlayer(p, sender.pos, sender.heading, sender.pitch);
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

            public override void Run(NewPlayer sender, string cmd, string args)
            {
                Spacecraft.Log(sender.name + " shut down the server");
                //NewServer.theServ.SendAll(NewPlayer.PacketKick("Server is shutting down!"));
                NewServer.OnExit.Set();
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

            public override void Run(NewPlayer sender, string cmd, string args)
            {
                NewServer.theServ.map.SetSpawn(sender.pos, sender.heading);
                sender.PrintMessage(Color.Teal + "Spawn point set");
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

            public override void Run(NewPlayer sender, string cmd, string args)
            {
                if (args == "")
                {
                    if (sender.placing)
                    {
                        sender.placing = false;
                        sender.PrintMessage(Color.Teal + "No longer placing");
                    }
                    else
                    {
                        sender.PrintMessage(Color.DarkRed + "No block specified");
                    }
                }
                else
                {
                    string b = args;
                    if (BlockInfo.NameExists(b))
                    {
                        sender.placing = true;
                        sender.placeType = BlockInfo.names[b];
                        sender.PrintMessage(Color.Teal + "Placing " + b + " in place of Obsidian. Use /place to cancel");
                    }
                    else
                    {
                        sender.PrintMessage(Color.DarkRed + "Unknown block " + b);
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

            public override void Run(NewPlayer sender, string cmd, string args)
            {
                if (args == "")
                {
                    sender.PrintMessage(Color.DarkRed + "No player specified");
                }
                else
                {
                    string pname = args;
                    NewPlayer p = NewServer.theServ.GetPlayer(pname);
                    if (p == null)
                    {
                        sender.PrintMessage(Color.DarkRed + "No such player " + pname);
                    }
                    else
                    {
                        NewServer.theServ.MovePlayer(sender, p.pos, p.heading, p.pitch);
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

            public override void Run(NewPlayer sender, string cmd, string args)
            {
                if (args == "")
                {
                    sender.PrintMessage(Color.DarkRed + "No player specified");
                }
                else
                {
                    string pname = args;
                    NewPlayer c = NewServer.theServ.GetPlayer(pname);
                    if (c == null)
                    {
                        sender.PrintMessage(Color.DarkRed + "No such player " + pname);
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

            public override void Run(NewPlayer sender, string cmd, string args)
            {
                if (args == "")
                {
                    sender.PrintMessage(Color.DarkRed + "No message specified");
                }
                else
                {
                    Spacecraft.Log("{" + sender.name + "} " + args);
                    NewServer.theServ.MessageAll(Color.Yellow + args);
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

            public override void Run(NewPlayer sender, string cmd, string args)
            {
                NewServer.theServ.map.Dehydrate();
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

            public override void Run(NewPlayer sender, string cmd, string args)
            {
                //NewServer.theServ.SpawnMob(sender._player, args);
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
                get { return "/resend: resend the map for testing purposes (brokenish)"; }
            }

            public override void Run(NewPlayer sender, string cmd, string args)
            {
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
                get { return "/rerank: reload ranks from the rank (mod+)"; }
            }

            public override void Run(NewPlayer sender, string cmd, string args)
            {
                Spacecraft.LoadRanks();
                Spacecraft.Log(sender.name + " reloaded the ranks");
                sender.PrintMessage(Color.Teal + "Ranks reloaded");
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

            public override void Run(NewPlayer sender, string cmd, string args)
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
                get { return "/config: manages raw server options"; }
            }

            public override void Run(NewPlayer sender, string cmd, string args)
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
                        sender.PrintMessage(Color.Teal + "No option " + argv[0] + " defined");
                    }
                    else
                    {
                        sender.PrintMessage(Color.Teal + "Option " + argv[0] + " is " + r);
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

            public override void Run(NewPlayer sender, string cmd, string args)
            {
				args = args.Trim().ToLower();
                Map map = NewServer.theServ.map;
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
                        NewServer.theServ.MovePlayer(sender, p, heading, 0);
                        sender.PrintMessage(Color.Teal + "Teleported to landmark " + args);
                    }
                    else
                    {
                        sender.PrintMessage(Color.DarkRed + "No such landmark " + args);
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

            public override void Run(NewPlayer sender, string cmd, string args)
            {
				args = args.Trim().ToLower();
                Map map = NewServer.theServ.map;
                if (args == "")
                {
                    string marks = "Landmarks: " + String.Join(", ", map.GetLandmarkList());
                    ChatCommandHandling.WrapMessage(sender, marks);
                }
                else
                {
                    if (map.landmarks.ContainsKey(args))
                    {
                        sender.PrintMessage(Color.DarkRed + "Landmark " + args + " already exists");
                    }
                    else
                    {
                        byte heading = sender.heading;
                        map.landmarks.Add(args, new Pair<Position, byte>(sender.pos, heading));
                        NewServer.theServ.MessageAll(Color.Yellow + sender.name + " created landmark " + args);
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

            public override void Run(NewPlayer sender, string cmd, string args)
            {
				args = args.Trim().ToLower();
                Map map = NewServer.theServ.map;
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
						NewServer.theServ.MessageAll(Color.Yellow + sender.name + " removed landmark " + args);
                    }
                    else
                    {
                        sender.PrintMessage(Color.DarkRed + "No such landmark " + args);
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

            public override void Run(NewPlayer sender, string cmd, string args)
            {
				args = args.Trim();
                NewPlayer p = NewServer.theServ.GetPlayer(args);
				Rank r = Player.LookupRank(args);
				if(p == null) {
					sender.PrintMessage(Color.Teal + args + " is offline");
					sender.PrintMessage(Color.Teal + args + " is a " + Player.RankColor(r) + r.ToString());
				} else {
					sender.PrintMessage(Color.Teal + args + " is online");
					sender.PrintMessage(Color.Teal + args + " is a " + Player.RankColor(r) + r.ToString());
					sender.PrintMessage(Color.Teal + args + " is at: " + p.pos.x + "," + p.pos.y + "," + p.pos.z);
				}
            }
        }

        public class Physics : ChatCommandBase
        {

            public override Rank RankNeeded
            {
                get { return Rank.Admin; }
            }

            public override string HelpMsg
            {
                get { return "/physics [true|false]: Enables/disables physics."; }
            }

            public override void Run(NewPlayer sender, string cmd, string arg)
            {
                if (arg != "")
                {
                    NewServer.theServ.map.PhysicsOn = (arg == "true");
                    NewServer.theServ.MessageAll(Color.Yellow + "Physics running - " + NewServer.theServ.map.PhysicsOn.ToString());
                }
                else
                {
                    NewServer.theServ.map.PhysicsOn = !NewServer.theServ.map.PhysicsOn;
                    NewServer.theServ.MessageAll(Color.Yellow + "Physics running - " + NewServer.theServ.map.PhysicsOn.ToString());
                }
            }
        }

        public class ConspiracyWizard : ChatCommandBase
        {
            public override Rank RankNeeded
            {
                get { return Rank.Admin; }
            }

            public override string HelpMsg
            {
                get { return "What does /conspiracywizard do? What DOESN'T it do?"; }
            }

            public override void Run(NewPlayer sender, string cmd, string arg)
            {
                Position oldpos = sender.pos;
                Position pos = new Position((short)(oldpos.x / 32), (short)(oldpos.y / 32), (short)(oldpos.z / 32));

                Map map = NewServer.theServ.map;

                int value = Spacecraft.random.Next(21, 36);

                for (short x = 0; x < map.xdim; x++)
                {
                    for (short y = 0; y < map.ydim; y++)
                    {
                        for (short z = 0; z < map.zdim; z++)
                        {
                            if (Math.Abs(pos.x - x) + Math.Abs(pos.y - y) + Math.Abs(pos.z - z) == 5)
                            {
                                NewServer.theServ.ChangeBlock(new BlockPosition(x, y, z),(Block) value);
                            }
                            if (Math.Abs(pos.x - x) + Math.Abs(pos.y - y) + Math.Abs(pos.z - z) < 5)
                            {
                                NewServer.theServ.ChangeBlock(new BlockPosition(x, y, z), Block.Air);
                            }
                        }
                    }
                }
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
                get { return "/convert [block] [repl]: Replace [block] with [repl]"; }
            }

            public override void Run(NewPlayer sender, string cmd, string arg)
            {
                

                string[] parts = arg.Split(new char[]{' '});
                if (parts.Length < 2)
                {
                    sender.PrintMessage(Color.CommandError + "Too few arguments!");
                    return;
                }

                Block To, From;
                try
                {
                    From = (Block)Enum.Parse(typeof(Block), parts[0]);
                    To = (Block)Enum.Parse(typeof(Block), parts[1]);
                }
                catch (ArgumentException)
                {
                    sender.PrintMessage(Color.CommandError + "No such block.");
                    return;
                }
                Map map = NewServer.theServ.map;
                
                int i = 0;
	            for (short x = 0; x < map.xdim; x++) {
	                for (short y = 0; y < map.ydim; y++) {
	                    for (short z = 0; z < map.zdim; z++) {
	                        if (map.GetTile(x, y, z) == From) ++i;
	                    }
	                } 
	            }
	            
				Spacecraft.Log(sender.name + " converted " + From.ToString() + " to " + To.ToString());
	            if(i > 500) {
	            	sender.PrintMessage(Color.CommandResult + "Converting max of 500 " + From.ToString() + " to " + To.ToString() + "...");
	            } else {
	                sender.PrintMessage(Color.CommandResult + "Converting " + i + " " + From.ToString() + " to " + To.ToString() + "...");
                }
	            map.ReplaceAll(From, To, 500);
            }
        }
    }
}