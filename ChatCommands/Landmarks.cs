using System;
using System.Collections.Generic;

namespace spacecraft {
	namespace ChatCommands {
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


        public class TeleportAdd : ChatCommandBase
        {
            public override Rank RankNeeded
            {
                get { return Rank.Mod; }
            }

            public override string HelpMsg
            {
                get { return "tp_add [name] [x] [y] [z] [heading]: Adds a teleport leading from current position to the given position."; }
            }

            public override void Run(Player sender, string cmd, string arg)
            {
                string[] parts = arg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5)
                {
                    sender.PrintMessage("Too few arguments!");
                    return;
                }

                string name = parts[0];
                byte x, y, z, heading;

                try
                {
                    x = byte.Parse(parts[1]);
                    y = byte.Parse(parts[2]);
                    z = byte.Parse(parts[3]);
                    heading = byte.Parse(parts[4]);
                }
                catch
                {
                    sender.PrintMessage("Invalid arguments!");
                    return;
                }

                Position Dest = new Position(x, y, z);
                Position Start = sender.pos;

                Start = new Position() { 
                    x = (short)(Start.x / 32),
                    y = (short)(Start.y / 32),
                    z = (short)(Start.z / 32)
                };

                Server.theServ.map.teleportDests.Add(Start, new Pair<Position, byte>(Dest, heading));
            }
        }

        public class TeleportRemove : ChatCommandBase
        {
            public override Rank RankNeeded
            {
                get { return Rank.Mod; }
            }

            public override string HelpMsg
            {
                get { return "tp_remove [name]: Remove the named teleporter.";}
            }

            public override void Run(Player sender, string cmd, string arg)
            {
                string name = arg;
                bool found = false;
                Position FoundKey = new Position() { x = 0, y = 0, z = 0 };

                foreach(var K in Server.theServ.map.teleportNames)
                {
                    if (K.Value == name)
                    {
                        found = true;
                        FoundKey = K.Key;
                    }
                }

                if (found)
                {
                    Server.theServ.map.teleportNames.Remove(FoundKey);
                    Server.theServ.map.teleportDests.Remove(FoundKey);
                    sender.PrintMessage("Removed teleport " + name);
                }
                else
                {
                    sender.PrintMessage("Teleport " + name + " does not exist.");
                }

            }
        }

	}
}