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
			Interpreter.CreateCommand("SetTile", new TclAPI.TclCommand(ScriptSetTile));
			Interpreter.CreateCommand("GetTile", new TclAPI.TclCommand(ScriptGetTile));
			Interpreter.CreateCommand("Broadcast", new TclAPI.TclCommand(ScriptBroadcast));
		}
		
		public static bool IsOk(int status) {
			return status != TclAPI.TCL_ERROR;
		}

		static int ScriptSetTile(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);

			if (argc != 5)
			{
				TclAPI.SetResult(interp, "Wrong number of arguments, expected 4, got " + argc.ToString());
				return TclAPI.TCL_ERROR;
			}

			short x = short.Parse(args[1]);
			short y = short.Parse(args[2]);
			short z = short.Parse(args[3]);
			
			args[4] = args[4].ToLower();

			if (!BlockInfo.NameExists(args[4]))
			{
				TclAPI.SetResult(interp, "Block name \"" + args[4] + "\" does not exist");
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
				TclAPI.SetResult(interp, "Wrong number of arguments, expected 3, got " + argc.ToString());
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
				TclAPI.SetResult(interp, "Wrong number of arguments, expected 1, got " + argc.ToString());
				return TclAPI.TCL_ERROR;
			}

			Server.theServ.MessageAll(Color.Announce + args[1]);

			TclAPI.SetResult(interp, "");
			return TclAPI.TCL_OK;
		}
	}
}
