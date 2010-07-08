using System;
using TclWrap;
using System.Text;

namespace spacecraft
{
	public class Scripting
	{
		static bool Initialized = false;

		public static TclInterpreter Interpreter { get; set; }

		public static void Initialize()
		{
			if (!Config.GetBool("tcl", false)) 
				return;
			
			if (Initialized) 
				return;

			Interpreter = new TclInterpreter();

			// Overwrite standard source, since it seems to crash :|
			Interpreter.CreateCommand("source", new TclAPI.TclCommand(ScriptEvalFile));

			// Spacecraft stuff
			Interpreter.CreateCommand("scLog", new TclAPI.TclCommand(ScriptLog));
			Interpreter.CreateCommand("setTile", new TclAPI.TclCommand(ScriptSetTile));
			Interpreter.CreateCommand("getTile", new TclAPI.TclCommand(ScriptGetTile));
			Interpreter.CreateCommand("broadcast", new TclAPI.TclCommand(ScriptBroadcast));
			Interpreter.CreateCommand("tell", new TclAPI.TclCommand(ScriptSendMessage));
			Interpreter.CreateCommand("players", new TclAPI.TclCommand(ScriptGetPlayers));
			Interpreter.CreateCommand("getPlayer", new TclAPI.TclCommand(ScriptGetPlayerStats));
			Interpreter.CreateCommand("setSpawn", new TclAPI.TclCommand(ScriptSetSpawnPoint));

			Initialized = true;
		}

		public static bool IsOk(int status) {
			return status != TclAPI.TCL_ERROR;
		}
		
		static int ScriptEvalFile(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);
			
			if (argc != 2) {
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + " fileName\"");
				return TclAPI.TCL_ERROR;
			}
			
			return Interpreter.SourceFile(args[1]);
		}
		
		static int ScriptLog(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);
			
			if (argc != 2) {
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + " text\"");
				return TclAPI.TCL_ERROR;
			}
			
			Spacecraft.Log("[S] " + args[1]);

			TclAPI.SetResult(interp, "");
			return TclAPI.TCL_OK;
		}

		static int ScriptSetTile(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);

            if (argc != 5 && argc != 6)
			{
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + "\" x y z type [fast]");
				return TclAPI.TCL_ERROR;
			}

			short x = short.Parse(args[1]);
			short y = short.Parse(args[2]);
			short z = short.Parse(args[3]);
			
			args[4] = args[4].ToLower();

			if (!BlockInfo.NameExists(args[4]))
			{
				TclAPI.SetResult(interp, "invalid block name \"" + args[4] + "\"");
				return TclAPI.TCL_ERROR;
			}

			Block B = BlockInfo.names[args[4]];
            bool slow = true;

            if (argc == 6)
                slow = bool.Parse(args[5]);

			Server.theServ.map.SetTile(x, y, z, B, slow);

			TclAPI.SetResult(interp, "");
			return TclAPI.TCL_OK;
		}

		static int ScriptGetTile(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);

			if (argc != 4)
			{
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + " x y z\"");
				return TclAPI.TCL_ERROR;
			}

			short x = short.Parse(args[1]);
			short y = short.Parse(args[2]);
			short z = short.Parse(args[3]);

			Block B = Server.theServ.map.GetTile(x, y, z);

			TclAPI.SetResult(interp, B.ToString());
			return TclAPI.TCL_OK;
		}

		static int ScriptBroadcast(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);

			if (argc != 2)
			{
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + " message\"");
				return TclAPI.TCL_ERROR;
			}

			Server.theServ.MessageAll(Color.Announce + args[1]);

			TclAPI.SetResult(interp, "");
			return TclAPI.TCL_OK;
		}

		static int ScriptSendMessage(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);

			if (argc != 3)
			{
				TclAPI.SetResult(interp, "Wrong number of arguments, expected 2, got " + argc.ToString());
				return TclAPI.TCL_ERROR;
			}

			Player P = Server.theServ.GetPlayer(args[1]);
			P.PrintMessage(args[2]);

			TclAPI.SetResult(interp, "");
			return TclAPI.TCL_OK;
		}

		static int ScriptGetPlayers(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			if (argc != 1)
			{
				TclAPI.SetResult(interp, "Wrong number of arguments, expected 0, got " + argc.ToString());
				return TclAPI.TCL_ERROR;
			}

			StringBuilder Result = new StringBuilder();

			foreach (Player P in Server.theServ.Players)
			{
				if (P != null)
				{
					Result.Append(P.name);
					Result.Append(" ");
				}
			}

			TclAPI.SetResult(interp, Result.ToString());
			return TclAPI.TCL_OK;

		}

		static int ScriptGetPlayerStats(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);

			if (argc != 2)
			{
				TclAPI.SetResult(interp, "Wrong number of arguments, expected 1, got " + argc.ToString());
				return TclAPI.TCL_ERROR;
			}

			Player Target = Server.theServ.GetPlayer(args[1]);

			StringBuilder Builder = new StringBuilder();
			Builder.Append(Target.playerID);
			Builder.Append(" ");
			Builder.Append(Target.pos.x);
			Builder.Append(" ");
			Builder.Append(Target.pos.y);
			Builder.Append(" ");
			Builder.Append(Target.pos.z);
			Builder.Append(" ");

			Builder.Append(Target.heading);
			Builder.Append(" ");
			Builder.Append(Target.pitch);
			Builder.Append(" ");

			Builder.Append(Target.rank);
			Builder.Append(" ");

			TclAPI.SetResult(interp, Builder.ToString());
			return TclAPI.TCL_OK;
		}
		
		static int ScriptRegisterChatCommand(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);

			if (argc != 3)
			{
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + " cmdName script\"");
				return TclAPI.TCL_ERROR;
			}

			//ChatCommandHandling.RegisterChatCommand(args[0], args[1]);

			TclAPI.SetResult(interp, "command isn't implemented");
			return TclAPI.TCL_ERROR;
		}

		static int ScriptSetSpawnPoint(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);

			if (argc != 5)
			{
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + " x y z\"");
				return TclAPI.TCL_ERROR;
			}

			short x = (short)(short.Parse(args[1]) * 32);
			short y = (short)(short.Parse(args[2]) * 32);
			short z = (short)(short.Parse(args[3]) * 32);
			byte heading = byte.Parse(args[4]);

			Server.theServ.map.SetSpawn(new Position(x,y,z), heading);

			TclAPI.SetResult(interp,"");
			return TclAPI.TCL_OK;
		}

	}
}
