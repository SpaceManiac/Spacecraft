using System;
using TclWrap;

namespace spacecraft
{
	public class Scripting
	{
		public static TclInterpreter Interpreter { get; set; }

		static Scripting()
		{
			Interpreter = new TclInterpreter();
			
			// Overwrite standard source, since it seems to crash :|
			Interpreter.CreateCommand("source", new TclAPI.TclCommand(ScriptEvalFile));
			
			// Basic actions
			Interpreter.CreateCommand("log", new TclAPI.TclCommand(ScriptLog));
			Interpreter.CreateCommand("setTile", new TclAPI.TclCommand(ScriptSetTile));
			Interpreter.CreateCommand("getTile", new TclAPI.TclCommand(ScriptGetTile));
			Interpreter.CreateCommand("broadcast", new TclAPI.TclCommand(ScriptBroadcast));
			
			// Callbacks
			Interpreter.CreateCommand("registerChatCommand", new TclAPI.TclCommand(ScriptRegisterChatCommand));
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

			if (argc != 5)
			{
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + "\" x y z type");
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

			Server.theServ.map.SetTile(x, y, z, B);

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
	}
}
