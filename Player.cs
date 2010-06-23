using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace spacecraft
{
	public class Player
	{
		public static Dictionary<String, Rank> PlayerRanks = new Dictionary<String, Rank>();
		public static List<byte> InUseIDs = new List<byte>();

		public delegate void PlayerSpawnHandler(Player sender);
		/// <summary>
		/// Triggered when the player first connects.
		/// </summary>
		public event PlayerSpawnHandler Spawn;

		public delegate void PlayerMoveHandler(Player player, Position dest, byte heading, byte pitch);
		/// <summary>
		/// Triggred when the player moves.
		/// </summary>
		public event PlayerMoveHandler Move;

		public delegate void PlayerBlockChangeHandler(BlockPosition pos, Block type);
		/// <summary>
		/// Triggred when the player changes a block.
		/// </summary>
		public event PlayerBlockChangeHandler BlockChange;

		public delegate void PlayerMsgHandler(string msg);
		/// <summary>
		/// Triggered when this client sends a message to the server.
		/// </summary>
		public event PlayerMsgHandler Message;

		public delegate void PlayerDisconnectHandler(Player sender);
		/// <summary>
		/// Triggered when this client disconnects from the server.
		/// </summary>
		public event PlayerDisconnectHandler Disconnect;

		private Connection conn;

		public Rank rank;
		public byte playerID;
		public string name { get; protected set; }
		public Position pos { get; protected set; }
		public byte heading { get; protected set; }
		public byte pitch { get; protected set; }

		public bool placing;
		public Block placeType;
		public bool painting;

		public Player(TcpClient client)
		{
			pos = Server.theServ.map.spawn;
			conn = new Connection(client);

			playerID = 0;
			for(byte i = 1; i <= 255; ++i) {
				if(!InUseIDs.Contains(i)) {
					playerID = i;
					break;
				}
			}
			if(playerID == 0) {
				throw new SpacecraftException("Bah, all out of player IDs");
			}

			conn.PlayerMove += new Connection.PlayerMoveHandler(conn_PlayerMove);
			conn.PlayerSpawn += new Connection.PlayerSpawnHandler(conn_PlayerSpawn);
			conn.ReceivedUsername += new Connection.UsernameHandler(conn_ReceivedUsername);
			conn.BlockSet += new Connection.BlockSetHandler(conn_BlockSet);
			conn.ReceivedMessage += new Connection.MessageHandler(conn_ReceivedMessage);
			conn.Disconnect += new Connection.DisconnectHandler(conn_Disconnect);
		}
		
		~Player() {
			InUseIDs.Remove(playerID);
		}
		
		public void Start()
		{
			conn.Start();
		}

		/// <summary>
		/// Should be called to inform the player that they've been kicked.
		/// </summary>
		public void Kick()
		{
			Kick("You were kicked!");
		}

		/// <summary>
		/// Should be called to inform the player that they've been kicked.
		/// </summary>
		/// <param name="reason">The reason</param>
		public virtual void Kick(string reason)
		{
			Server.theServ.MessageAll(Color.Yellow + name + " was kicked (" + reason + ")");
			conn.SendKick(reason);
		}

		/// <summary>
		/// Called by the server to inform the player they've been moved.
		/// </summary>
		/// <param name="dest">Where they moved to</param>

		/// <summary>
		/// Used to show messages to the player in the chat box, for chat messages, and the like.
		/// </summary>
		/// <param name="msg"></param>
		public virtual void PrintMessage(string msg)
		{
			conn.DisplayMessage(msg);
		}
		
		public void UpdateRank(Rank newRank)
		{
			conn.SendOperator(RankInfo.IsOperator(newRank));
			SetRankOf(name, newRank);
			rank = newRank;
			PrintMessage(Color.PrivateMsg + "You are now a " + RankInfo.RankColor(rank));
			PrintMessage(Color.PrivateMsg + " (note: any building commands have been reset)");
			placing = false;
			painting = false;
			placeType = Block.Undefined;
		}	

		public virtual void PlayerJoins(Player Player)
		{
			conn.HandlePlayerSpawn(Player, Player == this);
		}

		public virtual void PlayerMoves(Player Player, Position dest, byte heading, byte pitch)
		{
			if (Player == this) {
				pos = dest;
			}

			conn.SendPlayerMovement(Player, dest, heading, pitch, Player == this);
		}

		public void PlayerDisconnects(Player P) {
			PlayerDisconnects(P.playerID);
		}

		public virtual void PlayerDisconnects(byte ID)
		{
			conn.SendPlayerDisconnect(ID);
		}

		public virtual void BlockSet(BlockPosition pos, Block type)
		{
			conn.SendBlockSet(pos.x, pos.y, pos.z, (byte)type);
		}

		/* ================================================================================
		 * Rank Stuff
		 * ================================================================================
		 */

		public static Rank RankOf(string name)
		{
			name = name.Trim().ToLower();
			if (PlayerRanks.ContainsKey(name)) {
				return PlayerRanks[name];
			} else {
				return Rank.Guest;
			}
		}
		
		public static void SetRankOf(string name, Rank rank) {
			name = name.Trim().ToLower();
			PlayerRanks[name] = rank;
		}
		
		public static void LoadRanks() {
			PlayerRanks.Clear();
			StreamReader Reader = new StreamReader("admins.txt");
			string[] Lines = Reader.ReadToEnd().Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			Reader.Close();

			foreach (string line in Lines) {
				string[] parts = line.Split('=');

				string rankStr = parts[0].Trim();
				rankStr = rankStr.Substring(0, 1).ToUpper() + rankStr.Substring(1, parts[0].Length - 1);
				Rank rank = (Rank) Enum.Parse(typeof(Rank), rankStr);

				string[] people = parts[1].Split(',');
				foreach(string name in people) {
					SetRankOf(name, rank);
				}
			}
			
			// re-save so things get combined if needed.
			SaveRanks();
		}
		
		public static void SaveRanks() {
			Dictionary<Rank, List<string>> names = new Dictionary<Rank, List<string>>();
		 
			foreach (Rank r in Enum.GetValues(typeof(Rank))) {
				names[r] = new List<string>();
			}
			
			foreach (KeyValuePair<string, Rank> kvp in PlayerRanks) {
				names[kvp.Value].Add(kvp.Key);
			}
			
			StreamWriter Writer = new StreamWriter("admins.txt");
			
			foreach(KeyValuePair<Rank, List<string>> kvp in names) {
				if(kvp.Key == Rank.Guest) continue;
				if(kvp.Value.Count > 0) {
					Writer.Write(kvp.Key.ToString());
					Writer.Write("=");
					Writer.Write(String.Join(",", kvp.Value.ToArray()));
					Writer.WriteLine();
				}
			}
			
			Writer.Close();
		}

		/* ================================================================================
		 * Event handlers
		 * ================================================================================
		 */

		void conn_ReceivedUsername(string username)
		{
			this.name = username;
			rank = RankOf(username);
			if(rank == Rank.Banned) {
				conn.SendKick("You're banned!");
			}
		}

		void conn_PlayerSpawn()
		{
			if(name == null) {
				Kick("You tried to spawn with a null username!");
				return;
			}
				
			PrintMessage(Color.PrivateMsg + "Welcome, " + name + "! You're a " + RankInfo.RankColor(rank) + rank.ToString());
			
			if (Spawn != null)
				Spawn(this);
		}

		void conn_PlayerMove(Position dest, byte heading, byte pitch)
		{
	   		if(pos == dest && this.heading == heading && this.pitch == pitch) return;

			pos = dest;
			this.heading = heading;
			this.pitch = pitch;

			// Echo event on to other listeners, e.g. the server.
			Move(this, dest, heading, pitch);
		}

		void conn_BlockSet(short X, short Y, short Z, byte Mode, byte Type)
		{
			Block type = (Block)Type;
			if(Mode == 0x00 && !painting) {
				// block destroyed
				type = Block.Air;
			} else {
				// block placed
				if(placing && type == Block.Obsidian) {
					type = placeType;
				}
				
				if(type == Block.Stair && Server.theServ.map.GetTile(X, (short)(Y - 1), Z) == Block.Stair) {
					BlockSet(new BlockPosition(X, Y, Z), Block.Air);
					type = Block.DoubleStair;
					--Y;
				}
			}

			if(BlockChange != null)
				BlockChange(new BlockPosition(X, Y, Z), type);
		}

		void conn_ReceivedMessage(string message)
		{
			HandleMessage(message);
		}
		
		protected void HandleMessage(string msg) {
			if (msg[0] == '@') {
				// private messages
				int i = msg.IndexOf(' ');
				string username = msg.Substring(1);
				string message = "";
				if (i > 0) {
					username = msg.Substring(1, i - 1);
					if (i != msg.Length - 1) {
						message = msg.Substring(i + 1);
					}
				}
				if (message != "") {
					Player P = Server.theServ.GetPlayer(username);
					if (P == null) {
						PrintMessage(Color.CommandError + "No such user " + username);
					} else {
						PrintMessage(Color.PrivateMsg + ">" + username + "> " + message);
						P.PrintMessage(Color.PrivateMsg + "[" + this.name + "] " + message);
					}
				}
			} else if (msg[0] == '/') {
				// command; process before sending onwards

				int i = msg.IndexOf(' ');
				string cmd = msg.Substring(1);
				string args = "";
				if (i > 0) {
					cmd = msg.Substring(1, i - 1);
					if (i != msg.Length - 1) {
						args = msg.Substring(i + 1);
					}
				}

				ChatCommandHandling.Execute(this, cmd, args);
			}
			else
			{
				if (Message != null) Message(name + ": " + msg);
			}
		}

		void conn_Disconnect()
		{
			if (Disconnect != null)
				Disconnect(this);
		}
	}
}