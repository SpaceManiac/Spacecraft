using System;

namespace spacecraft
{
	/// <summary>
	/// Packets that are being sent by the server.
	/// </summary>
	public abstract class ServerPacket : Packet
	{
	}

	/// <summary>
	/// Confirms to the client that the player has been connected.
	/// </summary>
	public class ServerIdentPacket : ServerPacket
	{
		public override byte PacketID { get { return 0x00; } }
		public byte Version;
		public string Name;
		public string MOTD;
		public byte Type;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(Version);
			b.Append(Packet.PackString(Name));
			b.Append(Packet.PackString(MOTD));
			b.Append(Type);
			return b.ToArray();
		}
	}

	/// <summary>
	/// Quoth documentation:
	/// "Sent to clients periodically. The only way a client can disconnect at the moment is to force
	/// it closed, which does not let the server know. The ping packet is used to determine if the connection is still open."
	/// </summary>
	public class PingPacket : ServerPacket
	{
		public override byte PacketID { get { return 0x01; } }

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			return b.ToArray();
		}
	}

	/// <summary>
	/// Notifies player of incoming level data.
	/// </summary>
	public class LevelInitPacket : ServerPacket
	{
		public override byte PacketID { get { return 0x02; } }

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			return b.ToArray();
		}
	}

	/// <summary>
	/// Contains level data.
	/// </summary>
	public class LevelChunkPacket : ServerPacket
	{
		public override byte PacketID { get { return 0x03; } }
		public short ChunkLength;
		public byte[] ChunkData;
		public byte PercentComplete;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(Packet.PackShort(ChunkLength));
			b.Append(ChunkData);
			b.Append(PercentComplete);
			return b.ToArray();
		}

	}

	/// <summary>
	/// Finalises the level data.
	/// </summary>
	public class LevelEndPacket : ServerPacket
	{
		public override byte PacketID { get { return 0x04; } }
		public short X;
		public short Y;
		public short Z;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(Packet.PackShort(X));
			b.Append(Packet.PackShort(Y));
			b.Append(Packet.PackShort(Z));
			return b.ToArray();
		}

	}

	public class PlayerSpawnPacket : ServerPacket
	{
		public override byte PacketID { get { return 0x07; } }
		public byte PlayerID;
		public string Name;
		public short X;
		public short Y;
		public short Z;
		public byte Heading;
		public byte Pitch;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(PlayerID);
			b.Append(Packet.PackString(Name));
			b.Append(Packet.PackShort(X));
			b.Append(Packet.PackShort(Y));
			b.Append(Packet.PackShort(Z));
			b.Append(Heading);
			b.Append(Pitch);
			return b.ToArray();
		}
	}

	public class SetBlockPacket : ServerPacket
	{
		public override byte PacketID { get { return 0x06; } }
		public short X;
		public short Y;
		public short Z;
		public byte Type;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(Packet.PackShort(X));
			b.Append(Packet.PackShort(Y));
			b.Append(Packet.PackShort(Z));
			b.Append(Type);
			return b.ToArray();
		}
	}

	public class PlayerMovePacket : ServerPacket
	{
		public override byte PacketID
		{
			get { return 0x08; }
		}
		public byte PlayerID;
		public short X;
		public short Y;
		public short Z;
		public byte Heading;
		public byte Pitch;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(PlayerID);
			b.Append(Packet.PackShort(X));
			b.Append(Packet.PackShort(Y));
			b.Append(Packet.PackShort(Z));
			b.Append(Heading);
			b.Append(Pitch);
			return b.ToArray();
		}
	}

	public class DespawnPacket : ServerPacket
	{
		public override byte PacketID
		{
			get { return 0x0c; }
		}
		public byte PlayerID;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(PlayerID);
			return b.ToArray();
		}
	}

	/// <summary>
	/// To informat the client of messages generated by other players, the server, etc.
	/// </summary>
	public class ServerMessagePacket : ServerPacket
	{
		public override byte PacketID { get { return 0x0d; } }
		public byte PlayerID;
		public string Message;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(PlayerID);
			b.Append(Packet.PackString(Message));
			return b.ToArray();
		}
	}

	/// <summary>
	/// Disconnects the player from the server.
	/// </summary>
	public class DisconnectPacket : ServerPacket
	{
		public override byte PacketID { get { return 0x0e; } }
		public string Reason;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(Packet.PackString(Reason));
			return b.ToArray();
		}
	}

	/// <summary>
	/// Informs the player of op/de-op.
	/// </summary>
	public class RankUpdatePacket : ServerPacket
	{
		public override byte PacketID { get { return 0x0f; } }
		public byte UserType;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(UserType);
			return b.ToArray();
		}
	}
}