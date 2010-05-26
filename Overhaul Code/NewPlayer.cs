using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace spacecraft
{
    public class NewPlayer
    {

        public static Dictionary<String, Rank> PlayerRanks = new Dictionary<String, Rank>();

        public delegate void PlayerSpawnHandler(NewPlayer sender, string username);
        public event PlayerSpawnHandler Spawn;

        public delegate void PlayerMoveHandler(Position dest, byte heading, byte pitch);
        public event PlayerMoveHandler Move;

        public delegate void PlayerMsgHandler(string msg);
        /// <summary>
        /// Triggered when this client sends a message to the server.
        /// </summary>
        public event PlayerMsgHandler Message;

        private NewConnection conn;

        public Rank currentRank;
        public byte playerID;
        public string name { get; protected set; }
        public Position pos { get; protected set; }
        public byte heading;
        public byte pitch;

        public NewPlayer(TcpClient client)
        {
            pos = new Position(128, 128, 128);
            conn = new NewConnection(client);


            conn.PlayerMove += new NewConnection.PlayerMoveHandler(conn_PlayerMove);
            conn.PlayerSpawn += new NewConnection.PlayerSpawnHandler(conn_PlayerSpawn);
            conn.ReceivedMessage += new NewConnection.MessageHandler(conn_ReceivedMessage);

         
        }

      

        /// <summary>
        /// Should be called to inform the player that they've been kicked.
        /// </summary>
        public void Kick()
        {
            Kick(null);
        }

        /// <summary>
        /// Should be called to inform the player that they've been kicked.
        /// </summary>
        /// <param name="reason">The reason</param>
        public void Kick(string reason)
        {
            conn.SendKick(reason);
        }

        /// <summary>
        /// Called by the server to inform the player they've been moved.
        /// </summary>
        /// <param name="dest">Where they moved to</param>
        public void Teleport(Position dest)
        {
            pos = dest;
            conn.SendPositionUpdate(dest);
        }

        /// <summary>
        /// Used to show messages to the player in the chat box, for chat messages, and the like.
        /// </summary>
        /// <param name="msg"></param>
        public void PrintMessage(string msg)
        {
            conn.DisplayMessage(msg);
        }

        /* ================================================================================
         * Event handlers
         * ================================================================================
         */

        void conn_PlayerSpawn(string username)
        {
            this.name = name;
            
            if (PlayerRanks.ContainsKey(name))
            {
                currentRank = PlayerRanks[name];
            }
            else
            {
                currentRank = Rank.Guest;
            }

            if (Spawn != null)
                Spawn(this, username);
        }

        void conn_PlayerMove(Position dest, byte heading, byte pitch)
        {
            pos = dest;
            this.heading = heading;
            this.pitch = pitch;

            // Echo event on to other listeners, e.g. the server.
            Move(dest, heading, pitch);
        }

        void conn_ReceivedMessage(string msg)
        {
            if (Message != null)
                Message(msg);
        }
        
    }

}