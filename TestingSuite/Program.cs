using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestingSuite
{
    class Program
    {
        static void Main(string[] args)
        {
            spacecraft.PlayerIDPacket P = new spacecraft.PlayerIDPacket();
            P.Username = new byte[1024];
            P.Username[0] = 0xEE;
            P.Username[1023] = 0xFF;

            P.Key = new byte[1024];
            P.Key[0] = 0xAA;
            P.Key[1023] = 0xBB;

            P.Version = 0x07;

            string s = BitConverter.ToString(P);
        }
    }
}
