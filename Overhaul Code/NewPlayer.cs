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

        private NewConnection conn;

        public Rank currentRank;
        public byte playerID;
        public string name { get; protected set; }
        public Position pos { get; protected set; }

        public byte heading;
        public byte pitch;
        /*        public bool placing;
            public byte placeType; */

        public NewPlayer(TcpClient client)
        {
            name = null;
            pos = new Position(128, 128, 128);
            conn = new NewConnection(client);

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

        public bool PositionUpdate(Int16 X, Int16 Y, Int16 Z, byte Heading, byte Pitch)
        {
            bool changed = false;
            Position newPos = new Position(X, Y, Z);
            if (newPos != pos)
            {
                changed = true;
                pos = newPos;
            }
            if (Heading != heading)
            {
                changed = true;
                heading = Heading;
            }
            if (Pitch != pitch)
            {
                changed = true;
                pitch = Pitch;
            }
            return changed;
        }

        public void Kick()
        {
            Kick("");
        }

        public void Kick(string reason)
        {
            conn.SendKick(reason);
        }
        public void Teleport(Position dest)
        {
            throw new NotImplementedException();
        }
        public void PrintMessage(string msg)
        {
            conn.DisplayMessage(msg);
        }

    }

}