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
            if (Initialized) return;

            Interpreter = new TclInterpreter();

            // Overwrite standard source, since it seems to crash :|
            Interpreter.CreateCommand("source", new TclAPI.TclCommand(ScriptEvalFile));

            // Spacecraft stuff
            Interpreter.CreateCommand("Log", new TclAPI.TclCommand(ScriptLog));
            Interpreter.CreateCommand("SetTile", new TclAPI.TclCommand(ScriptSetTile));
            Interpreter.CreateCommand("GetTile", new TclAPI.TclCommand(ScriptGetTile));
            Interpreter.CreateCommand("Broadcast", new TclAPI.TclCommand(ScriptBroadcast));
            Interpreter.CreateCommand("tell", new TclAPI.TclCommand(ScriptSendMessage));
            Interpreter.CreateCommand("players", new TclAPI.TclCommand(ScriptGetPlayers));
            Interpreter.CreateCommand("getplayer", new TclAPI.TclCommand(ScriptGetPlayerStats));

            Initialized = true;
        }


		public static bool IsOk(int status) {
			return status != TclAPI.TCL_ERROR;
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

		
		static int ScriptEvalFile(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);
			
			if (argc != 2) {
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + " fileName\"");
				return TclAPI.TCL_ERROR;
			}
			
			return Interpreter.SourceFile(args[1]);
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
            string[] args = TclAPI.GetArgumentArray(argc, argsPtr);

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

        }

	}
}
