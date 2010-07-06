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
		public NetworkString Name;
		public NetworkString MOTD;
		public byte Type;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(Version);
			b.Append(Name);
			b.Append(MOTD);
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
		public NetworkShort ChunkLength;
		public NetworkByteArray ChunkData;
		public byte PercentComplete;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(ChunkLength);
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
		public NetworkShort X;
		public NetworkShort Y;
		public NetworkShort Z;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(X);
			b.Append(Y);
			b.Append(Z);
			return b.ToArray();
		}

	}

	public class PlayerSpawnPacket : ServerPacket
	{
		public override byte PacketID { get { return 0x07; } }
		public byte PlayerID;
		public NetworkString Name;
		public NetworkShort X;
		public NetworkShort Y;
		public NetworkShort Z;
		public byte Heading;
		public byte Pitch;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(PlayerID);
			b.Append(Name);
			b.Append(X);
			b.Append(Y);
			b.Append(Z);
			b.Append(Heading);
			b.Append(Pitch);
			return b.ToArray();
		}
	}

	public class SetBlockPacket : ServerPacket
	{
		public override byte PacketID { get { return 0x06; } }
		public NetworkShort X;
		public NetworkShort Y;
		public NetworkShort Z;
		public byte Type;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(X);
			b.Append(Y);
			b.Append(Z);
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
		public NetworkShort X;
		public NetworkShort Y;
		public NetworkShort Z;
		public byte Heading;
		public byte Pitch;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(PlayerID);
			b.Append(X);
			b.Append(Y);
			b.Append(Z);
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
		public NetworkString Message;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(PlayerID);
			b.Append(Message);
			return b.ToArray();
		}
	}

	/// <summary>
	/// Disconnects the player from the server.
	/// </summary>
	public class DisconnectPacket : ServerPacket
	{
		public override byte PacketID
		{
			get { return 0x0e; }
		}
		public NetworkString Reason;

		public override byte[] ToByteArray()
		{
			Builder<byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(Reason);
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