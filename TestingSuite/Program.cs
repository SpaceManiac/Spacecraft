using System;
using System.Collections.Generic;
using System.Text;
using spacecraft;

namespace spacecraft
{
	class Program
	{
		static void Main()
		{
			PositionUpdatePacket p = new PositionUpdatePacket();
			p.Heading = 197;
			p.Pitch = 117;
			p.PlayerID = 65;
			p.X = (short) 19;
			p.Y = (short) 85;
			p.Z = (short) 147;
			byte[] b = p.ToByteArray();
			Console.WriteLine(b);

		}
	}
}
