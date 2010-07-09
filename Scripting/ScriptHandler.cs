using System;
using TclWrap;
using System.Text;
using System.Collections.Generic;

namespace spacecraft
{
	public class Scripting
	{
		public static bool Initialized { get; protected set; }
		public static TclInterpreter Interpreter { get; set; }
		private static Dictionary<string, List<string>> Hooks;
		
		static Scripting() {
			Initialized = false;
		}

		public static void Initialize()
		{
			if (!Config.GetBool("tcl", false))
				return;
			
			if (Initialized) 
				return;

			Interpreter = new TclInterpreter();
			Hooks = new Dictionary<string, List<string>>();

			// Overwrite standard source, since it seems to crash :|
			Interpreter.CreateCommand("source", new TclAPI.TclCommand(ScriptEvalFile));
			
			// 1. Get static info
			Interpreter.CreateCommand("getBlockName", new TclAPI.TclCommand(ScriptGetBlockName));
			Interpreter.CreateCommand("getBlockID", new TclAPI.TclCommand(ScriptGetBlockID));
			Interpreter.CreateCommand("getColorCode", new TclAPI.TclCommand(ScriptGetColorCode));
			
			// 2. Get info on the world
			Interpreter.CreateCommand("getTile", new TclAPI.TclCommand(ScriptGetTile));
			Interpreter.CreateCommand("playerList", new TclAPI.TclCommand(ScriptPlayerList));
			Interpreter.CreateCommand("playerInfo", new TclAPI.TclCommand(ScriptGetPlayerInfo));

			// 3. Affect the world
			Interpreter.CreateCommand("scLog", new TclAPI.TclCommand(ScriptLog));
			Interpreter.CreateCommand("broadcast", new TclAPI.TclCommand(ScriptBroadcast));
			Interpreter.CreateCommand("setTile", new TclAPI.TclCommand(ScriptSetTile));
			Interpreter.CreateCommand("tell", new TclAPI.TclCommand(ScriptSendMessage));
			Interpreter.CreateCommand("setSpawn", new TclAPI.TclCommand(ScriptSetSpawnPoint));
			
			// 4. Register callbacks
			Interpreter.CreateCommand("createChatCommand", new TclAPI.TclCommand(ScriptRegisterChatCommand));
			Interpreter.CreateCommand("onLevelGeneration", new TclAPI.TclCommand(ScriptGenericHook));
			Interpreter.CreateCommand("onWorldTick", new TclAPI.TclCommand(ScriptGenericHook));
			Interpreter.CreateCommand("dropHook", new TclAPI.TclCommand(ScriptDropHook));

			Spacecraft.Log("Reading startup.tcl...");
			int status = Interpreter.SourceFile("Scripting/startup.tcl");
			if (!IsOk(status)) {
				Spacecraft.Log("Error in startup.tcl: " + Interpreter.Result);
			}
			Spacecraft.Log("Tcl initialized");
			
			Initialized = true;
		}

		public static bool IsOk(int status) {
			return status != TclAPI.TCL_ERROR;
		}
		
		public static bool HookDefined(string hookName) {
			return Hooks.ContainsKey(hookName) && Hooks[hookName].Count > 0;
		}
		
		public static int ExecuteHook(string hookName, string arguments) {
			foreach(string command in new List<String>(Hooks[hookName])) {
				if(!IsOk(Interpreter.EvalScript(command + " " + arguments))) {
					return TclAPI.TCL_ERROR;
				}
			}
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
		
		static int ScriptGetBlockName(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			// syntax: getBlockName blockID
			// help: returns the block name for the specified _blockID_ byte
			
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);
			
			if (argc != 2) {
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + " blockID\"");
				return TclAPI.TCL_ERROR;
			}
			
			try {
				int i = int.Parse(args[1]);
				if(i < 0 || i > (int)Block.Obsidian) {
					TclAPI.SetResult(interp, "block id out of range");
					return TclAPI.TCL_ERROR;
				} else {
					Block b = (Block) i;
					TclAPI.SetResult(interp, b.ToString().ToLower());
					return TclAPI.TCL_OK;
				}
			}
			catch(ArgumentException) {
				TclAPI.SetResult(interp, "expected integer instead of \"" + args[1] + "\"");
				return TclAPI.TCL_ERROR;
			}
		}
		
		static int ScriptGetBlockID(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			// syntax: getBlockID blockName
			// help: returns the block ID byte for the specified _blockName_
			
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);
			
			if (argc != 2) {
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + " blockName\"");
				return TclAPI.TCL_ERROR;
			}
			
			try {
				int i = (int) (Block) Enum.Parse(typeof(Block), args[1], true);
				TclAPI.SetResult(interp, i.ToString());
				return TclAPI.TCL_OK;
			}
			catch(ArgumentException) {
				TclAPI.SetResult(interp, "unknown block name \"" + args[1] + "\"");
				return TclAPI.TCL_ERROR;
			}
		}
		
		static string ScriptHelper_GetColorCode(string s) {
			switch(s) {
				case "black": return Color.Black;
				case "darkblue": return Color.DarkBlue;
				case "darkgreen": return Color.DarkGreen;
				case "darkteal": return Color.DarkTeal;
				case "darkred": return Color.DarkRed;
				case "purple": return Color.Purple;
				case "darkyellow": return Color.DarkYellow;
				case "gray": return Color.Gray;
				case "grey": return Color.Gray;
				case "darkgray": return Color.DarkGray;
				case "darkgrey": return Color.DarkGray;
				case "blue": return Color.Blue;
				case "green": return Color.Green;
				case "teal": return Color.Teal;
				case "red": return Color.Red;
				case "pink": return Color.Pink;
				case "yellow": return Color.Yellow;
				case "white": return Color.White;
				
				case "announce": return Color.Announce;
				case "privatemsg": return Color.PrivateMsg;
				case "commandresult": return Color.CommandResult;
				case "commanderror": return Color.CommandError;
				
				default: return "";
			}
		}
		
		static int ScriptGetColorCode(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			// syntax: getColorCode colorName
			// help: returns the &x color code for the specified _colorName_ (special names are announce, privatemsg, commandresult, and commanderror)
			
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);
			
			if (argc != 2) {
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + " colorName\"");
				return TclAPI.TCL_ERROR;
			}
			
			string s = ScriptHelper_GetColorCode(args[1].ToLower());
			
			if (s != "") {
				TclAPI.SetResult(interp, s);
				return TclAPI.TCL_OK;
			} else {
				TclAPI.SetResult(interp, "unknown color name \"" + args[1] + "\"");
				return TclAPI.TCL_ERROR;
			}
		}
		
		static int ScriptLog(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			// syntax: scLog text
			// help: Log _text_ with the [S] designation to Spacecraft's log files
			
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
			// syntax: setTile x y z type _?fast?_
			// help: Set a tile (_type_ should be a string). If _fast_ is enabled, the block will be set but no updates will be sent.
			
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);

			if (argc != 5 && argc != 6)
			{
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + "\" x y z type ?fast?");
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

			if(slow) {
				Server.theServ.map.SetTile(x, y, z, B);
			} else {
				Server.theServ.map.SetTile_Fast(x, y, z, B);
			}

			TclAPI.SetResult(interp, "");
			return TclAPI.TCL_OK;
		}

		static int ScriptGetTile(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			// syntax: getTile x y z
			// help: Returns the block type of a location in string form.
			
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

			TclAPI.SetResult(interp, B.ToString().ToLower());
			return TclAPI.TCL_OK;
		}

		static int ScriptBroadcast(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			// syntax: broadcast message
			// help: Broadcast a message in yellow text, a la /say
			
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
			// syntax: tell playerName message
			// help: Send a message with no extra coloring to an individual player.
			
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);

			if (argc != 3)
			{
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + " playerName message\"");
				return TclAPI.TCL_ERROR;
			}

			Player P = Server.theServ.GetPlayer(args[1]);
			
			if(P == null) {
				TclAPI.SetResult(interp, "no such player \"" + args[1] + "\"");
				return TclAPI.TCL_ERROR;
			}
			
			P.PrintMessage(args[2]);

			TclAPI.SetResult(interp, "");
			return TclAPI.TCL_OK;
		}

		static int ScriptPlayerList(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			// syntax: playerList
			// help: Returns a list of all players online.
			
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);
			
			if (argc != 1)
			{
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + "\"");
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

		static int ScriptGetPlayerInfo(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			// syntax: playerInfo playerName
			// help: Returns a list of player info in the form {id x y z heading pitch rank}
			
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);

			if (argc != 2)
			{
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + " playerName\"");
				return TclAPI.TCL_ERROR;
			}

			Player Target = Server.theServ.GetPlayer(args[1]);
			
			if(Target == null) {
				TclAPI.SetResult(interp, "no such player \"" + args[1] + "\"");
				return TclAPI.TCL_ERROR;
			}

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
			// syntax: createChatCommand commandName rankNeeded help script
			// help: Registers a new chat command. The _script_ will be called with the sender's name and the rest of the arguments.
			
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);

			if (argc != 5)
			{
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + " cmdName rankNeeded help script\"");
				return TclAPI.TCL_ERROR;
			}

			try {
				ChatCommandHandling.AddTclCommand(args[1], args[2], args[3], args[4]);
			}
			catch(Exception e) {
				TclAPI.SetResult(interp, "exception: " + e.Message);
				return TclAPI.TCL_ERROR;
			}

			TclAPI.SetResult(interp, "");
			return TclAPI.TCL_OK;
		}

		static int ScriptSetSpawnPoint(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			// syntax: setSpawn x y z
			// help: Sets the spawn point to {_x y z_} in terms of block coordinates.
			
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

			TclAPI.SetResult(interp, "");
			return TclAPI.TCL_OK;
		}

		static int ScriptGenericHook(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			// syntax: (hookname) command
			// help: Registers a hook for (hookname). See the Hooks section.
			
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);

			if (argc != 2)
			{
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + " command\"");
				return TclAPI.TCL_ERROR;
			}
			
			if(!Hooks.ContainsKey(args[0])) {
				Hooks.Add(args[0], new List<string>());
			}
			Hooks[args[0]].Add(args[1]);
			
			Spacecraft.Log("Hook defined: " + args[0]);

			TclAPI.SetResult(interp, "");
			return TclAPI.TCL_OK;
		}
		
		static int ScriptDropHook(IntPtr clientData, IntPtr interp, int argc, IntPtr argsPtr)
		{
			// syntax: dropHook hookName command
			// help: Drops the specified hook from _hookName_. See the Hooks section.
			
			string[] args = TclAPI.GetArgumentArray(argc, argsPtr);

			if (argc != 3)
			{
				TclAPI.SetResult(interp, "wrong # args: should be \"" + args[0] + " hookName command\"");
				return TclAPI.TCL_ERROR;
			}
			
			string hook = args[1];
			string cmd = args[2];
			
			if(Hooks.ContainsKey(hook) && Hooks[hook].Contains(cmd)) {
				Hooks[hook].Remove(cmd);
				if(Hooks[hook].Count == 0) {
					Hooks.Remove(hook);
				}
				Spacecraft.Log("Hook dropped: " + args[1]);
				TclAPI.SetResult(interp, "");
				return TclAPI.TCL_OK;
			} else {
				TclAPI.SetResult(interp, "hook \"" + hook + "\" does not have command \"" + cmd + "\" attached");
				return TclAPI.TCL_ERROR;
			}
		}
	}
}
