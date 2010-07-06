using System;

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
	}
}