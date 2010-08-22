using System;

namespace spacecraft
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

	public static class PacketLengthInfo
	{
		public static int Lookup(PacketType p)
		{
			string name = Enum.GetName(typeof(PacketType), p);
			return (int)Enum.Parse(typeof(PacketLength), name);
		}
	}
}