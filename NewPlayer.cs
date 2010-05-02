using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace spacecraft
{

    class NewPlayer
    {
        public static Dictionary<String, RankEnum> PlayerRanks = new Dictionary<string, RankEnum>(); 
        /// <summary>
        /// Translates a name into a given rank.
        /// i.e. PlayerRanks["Blocky"] = RankEnum.Admin;
        /// </summary>
        public static Dictionary<Byte, NewPlayer> idTable =  new Dictionary<Byte,NewPlayer>();

        private Connection conn;


        public RankEnum rank;
        public Byte playerID;
        public string name { get; protected set; }
        public Position pos { get; protected set; }
        

        public byte heading;
        public byte pitch;
        public bool placing;
        public byte placeType;

        public NewPlayer(TcpClient client, string username)
        {
            name = username;
            pos = new Position(128, 128, 128);
            conn = new Connection(client );

            for (Byte i = 0; i < Byte.MaxValue; i++)
            {
                if (!idTable.ContainsKey(i))
                {
                    idTable.Add(i, this);
                    this.playerID = i;
                    break;
                }
            }

            rank = RankEnum.Guest;
            if (PlayerRanks.ContainsKey(name))
                rank = PlayerRanks[name];

        }

        ~NewPlayer()
        {
            idTable.Remove(playerID);
        }

        public bool PositionUpdate(Int16 X, Int16 Y, Int16 Z, byte Heading, byte Pitch)
        {
            bool changed = false;
            Position newPos = new Position(X, Y, Z);
            if (newPos != pos)
            {
                changed = true;
                pos = newPos;
            }
            if (Heading == this.heading)
            {
                changed = true;
                this.heading = Heading;
            }
            if (Pitch == this.pitch)
            {
                changed = true;
                this.pitch = Pitch;
            }
            return changed;
        }

    }
}
