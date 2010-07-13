using System;

namespace spacecraft {
	namespace ChatCommands {
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
				for (short x = (short) (pos.x - 20); x <= pos.x + 20; x++) {
					for (short y = (short) (pos.y - 20); y <= pos.y + 20; y++) {
						for (short z = (short) (pos.z - 20); z <= pos.z + 20; z++) {
							if (map.GetTile(x, y, z) == From && (Math.Abs(pos.x - x) + Math.Abs(pos.y - y) + Math.Abs(pos.z - z) < 20)) {
								++i;
								map.SetTile(x, y, z, To);
							}
						}
					}
				}

				sender.PrintMessage(Color.CommandResult + "Converted " + i.ToString() + " " + From.ToString() + " to " + To.ToString());
				Spacecraft.Log(sender.name + " converted " + From.ToString().ToLower() + " to " + To.ToString().ToLower());
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
	}
}