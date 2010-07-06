using System;

namespace spacecraft
{
	public partial class Packet
	{
		public enum PacketType : byte
		{
			UNKNOWN = 0xFF, // Used by PacketFromLength, when the length does not match any packet.
			Ident = 0x00, // client & server
			Ping = 0x01, // server
			LevelInit = 0x02, // server
			LevelChunk = 0x03, // server
			LevelFinish = 0x04, // server
			PlayerSetBlock = 0x05, // client
			ServerSetBlock = 0x06, // server
			SpawnPlayer = 0x07, // server
			PositionUpdate = 0x08, // client & server
			U_PositionUpdate = 0x09, // unused server
			U_PositionUpdate2 = 0x0a, // unused server
			U_OrientUpdate = 0x0b, // unused server
			DespawnPlayer = 0x0c, // server
			Message = 0x0d, // client & server
			Kick = 0x0e, // server
		}

		public enum PacketLength : int
		{
			Ident = 131, // client & server
			Ping = 1, // server
			LevelInit = 1, // server
			LevelChunk = 1028, // server
			LevelFinish = 7, // server
			PlayerSetBlock = 9, // client
			ServerSetBlock = 8, // server
			SpawnPlayer = 74, // server
			PositionUpdate = 10, // client & server
			U_PositionUpdate = 10, // unused server
			U_PositionUpdate2 = 8, // unused server
			U_OrientUpdate = 4, // unused server
			DespawnPlayer = 2, // server
			Message = 66, // client & server
			Kick = 65, // server
		}

		static public PacketType PacketFromLength(int length)
		{
			PacketType val;
			// TODO: Make this better.

			switch (length)
			{
				case 131:
					val= PacketType.Ident;
					break;
				case 1:
					val = PacketType.Ping;
					// LevelInit is /also/ one byte long, but there should be no reason we need to identify it by it's length.
					break;
				case 1028:
					val = PacketType.LevelChunk;
					break;
				case 7:
					val = PacketType.LevelFinish;
					break;
				case 9:
					val = PacketType.PlayerSetBlock;
					break;
				case 8:
					val = PacketType.ServerSetBlock;
					break;
				// Ditto PacketType.U_PositionUpdate2;
				case 74:
					val = PacketType.SpawnPlayer;
					break;
				case 10:
					val = PacketType.PositionUpdate;
					break;
					// Ditto U_PositionUpdate;
				case 4:
					val = PacketType.U_OrientUpdate;
					break;
				case 2:
					val = PacketType.DespawnPlayer;
					break;
				case 66:
					val = PacketType.Message;
					break;
				case 65:
					val = PacketType.Kick;
					break;
				default:
					 val = PacketType.UNKNOWN;
					 break;
			}

			return val;
		}
	}

	public static class PacketLengthInfo
	{
		public static int Lookup(Packet.PacketType p)
		{
			string name = Enum.GetName(typeof(Packet.PacketType), p);
			return (int)Enum.Parse(typeof(Packet.PacketLength), name);
		}
	}
}