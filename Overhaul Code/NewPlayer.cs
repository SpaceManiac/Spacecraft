using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace spacecraft
{
    public class NewPlayer
    {

        public static Dictionary<String, Rank> PlayerRanks = new Dictionary<String, Rank>();

        public delegate void PlayerSpawnHandler(NewPlayer sender);
        /// <summary>
        /// Triggered when the player first connects.
        /// </summary>
        public event PlayerSpawnHandler Spawn;

        public delegate void PlayerMoveHandler(NewPlayer player, Position dest, byte heading, byte pitch);
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

        public delegate void PlayerDisconnectHandler(NewPlayer sender);
        /// <summary>
        /// Triggered when this client disconnects from the server.
        /// </summary>
        public event PlayerDisconnectHandler Disconnect;

        private NewConnection conn;

        public Rank rank;
        public byte playerID;
        public string name { get; protected set; }
        public Position pos { get; protected set; }
        public byte heading { get; protected set; }
        public byte pitch { get; protected set; }
        
        public bool placing;
        public Block placeType;

        public NewPlayer(TcpClient client, byte ID)
        {
            pos = NewServer.theServ.map.spawn;
            conn = new NewConnection(client);

            playerID = ID;

            conn.PlayerMove += new NewConnection.PlayerMoveHandler(conn_PlayerMove);
            conn.PlayerSpawn += new NewConnection.PlayerSpawnHandler(conn_PlayerSpawn);
            conn.BlockSet += new NewConnection.BlockSetHandler(conn_BlockSet);
            conn.ReceivedUsername += new NewConnection.UsernameHandler(conn_ReceivedUsername);
            conn.ReceivedMessage += new NewConnection.MessageHandler(conn_ReceivedMessage);
            conn.Disconnect += new NewConnection.DisconnectHandler(conn_Disconnect);
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
        public void Kick(string reason)
        {
            NewServer.theServ.MessageAll(Color.Yellow + name + " was kicked (" + reason + ")");
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
        public void PrintMessage(string msg)
        {
            conn.DisplayMessage(msg);
        }

        public void PlayerJoins(NewPlayer Player)
        {
            conn.HandlePlayerSpawn(Player, Player == this);
        }

        public void PlayerMoves(NewPlayer Player, Position dest, byte heading, byte pitch)
        {
            byte ID = Player.playerID;
            if (Player == this)
            {
                ID = 255;
                pos = dest;
            }

            conn.SendPlayerMovement(Player, dest, heading, pitch, Player == this);
        }

        public void PlayerDisconnects(NewPlayer P) { PlayerDisconnects(P.playerID); }

        public void PlayerDisconnects(byte ID)
        {
            conn.SendPlayerDisconnect(ID);
        }

		public void BlockSet(BlockPosition pos, Block type)
		{
			conn.SendBlockSet(pos.x, pos.y, pos.z, (byte)type);
		}
		
		/* ================================================================================
		 * Event handlers
         * ================================================================================
         */

        void conn_ReceivedUsername(string username)
        {
            this.name = username;

            if (PlayerRanks.ContainsKey(name)) {
                rank = PlayerRanks[name];
            } else {
                rank = Rank.Guest;
            }
        }


        void conn_PlayerSpawn()
        {
            if(name == null)
            	throw new Exception("tonoes?");
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
			if(Mode == 0x00) {
				// block destroyed
				type = Block.Air;
			} else {
				// block placed
				if(placing && type == Block.Obsidian) {
					type = placeType;
				}
			}
			
			if(BlockChange != null)
				BlockChange(new BlockPosition(X, Y, Z), type);
		}

        void conn_ReceivedMessage(string msg)
        {
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
                    NewPlayer P = NewServer.theServ.GetPlayer(username);
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