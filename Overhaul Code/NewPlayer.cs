using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace spacecraft
{
    class NewPlayer
    {

        public static Dictionary<String, Rank> PlayerRanks = new Dictionary<String, Rank>();
        public static Dictionary<Byte, NewPlayer> idTable = new Dictionary<Byte, NewPlayer>();

        public delegate void PlayerSpawnHandler(string username);
        public event PlayerSpawnHandler PlayerSpawn;

        public delegate void PlayerMoveHandler(Position dest);
        public event PlayerMoveHandler PlayerMove;

        private NewConnection conn;

        public Rank currentRank;
        public byte playerID;
        public string name { get; protected set; }
        public Position pos { get; protected set; }
        public byte heading;
        public byte pitch;

        public NewPlayer(TcpClient client)
        {
            name = null;
            pos = new Position(128, 128, 128);
            conn = new NewConnection(client);

            conn.PlayerMove += new NewConnection.PlayerMoveHandler(conn_PlayerMove);
            conn.PlayerSpawn += new NewConnection.PlayerSpawnHandler(conn_PlayerSpawn);


            for (byte i = 0; i < byte.MaxValue; ++i)
            {
                if (!idTable.ContainsKey(i))
                {
                    idTable.Add(i, this);
                    this.playerID = i;
                    break;
                }
            }

            if (PlayerRanks.ContainsKey(name))
            {
                currentRank = PlayerRanks[name];
            }
            else
            {
                currentRank = Rank.Guest;
            }
        }

       

        ~NewPlayer()
        {
            idTable.Remove(playerID);
        }

        public void Kick()
        {
            Kick(null);
        }

        public void Kick(string reason)
        {
            conn.SendKick(reason);
        }

        public void Teleport(Position dest)
        {
            pos = dest;

            conn.SendPositionUpdate(dest);
        }

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
        }

        void conn_PlayerMove(Position dest, byte heading, byte pitch)
        {
            pos = dest;
            this.heading = heading;
            this.pitch = pitch;
        }

        
    }

}