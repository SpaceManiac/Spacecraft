using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using spacecraft;

namespace TestingSuite
{
    class Program
    {
        static void Main(string[] args)
        {
			Console.WriteLine("== Testing packets ==");
            PlayerIDPacket P = new PlayerIDPacket();
            P.Username = new byte[1024];
            P.Username[0] = 0xEE;
            P.Username[1023] = 0xFF;

            P.Key = new byte[1024];
            P.Key[0] = 0xAA;
            P.Key[1023] = 0xBB;

            P.Version = 0x07;

            string s = BitConverter.ToString(P);
			Console.WriteLine("PlayerIDPacket: " + s);
			
			Console.WriteLine();
			Console.WriteLine("== Testing EscherMode ==");
			
			BlockPosition pos = new BlockPosition(8, 16, 24);
			foreach(EscherMode mode in Enum.GetValues(typeof(EscherMode))) {
				string name = Enum.GetName(typeof(EscherMode), mode);
				BlockPosition pos2 = EscherMath.CoordsTo(null, pos, mode);
				BlockPosition pos3 = EscherMath.CoordsFrom(null, pos2, mode);
				Console.WriteLine("{0}:\t{1}\t=>\t{2}\t=>\t{3}\t{4}", name, pos, pos2, pos3, (pos3 == pos ? "matching" : "not matching"));
			}
			
			foreach(EscherMode mode in Enum.GetValues(typeof(EscherMode))) {
				Console.WriteLine("{0}: {1} returned from RandomMode successfully", Enum.GetName(typeof(EscherMode), mode), EscherMath.RandomMode().ToString());
			}
        }
    }
}
