using System;
using System.Text;
using System.Net;

namespace spacecraft
{
	public abstract class Packet  // Continued in Constants.cs, which defines enums.
	{
		abstract public byte PacketID { get; }

		abstract public byte[] ToByteArray();
		
		// Unpack string and short from byte array
		public static string ExtractString(byte[] bytes, int offset) {
			// Find last non-space.
			for (int i = 63; i >= 0; --i) {
				if (bytes[offset + i] != (byte)(' ')) {
					// Return string up to and including that non-space.
					return Encoding.ASCII.GetString(bytes, offset, i + 1);
				}
			}
			return "";
		}
		
		public static short ExtractShort(byte[] bytes, int offset) {
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bytes, offset));
		}
		
		// Pack string and short into byte array
		public static byte[] PackString(string str) {
			byte[] result = new byte[64];
			for (int i = 0; i < 64; ++i) {
				result[i] = (byte)(' ');
			}
			Array.Copy(Encoding.ASCII.GetBytes(str), result, str.Length);
			return result;
		}
		
		public static byte[] PackShort(short val) {
			return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(val));
		}
	}

	/// <summary>
	/// Packets that are being sent by the client, received by the server.
	/// </summary>
	public abstract class ClientPacket : Packet
	{
		public static ClientPacket FromByteArray(byte[] array)
		{
			switch ((PacketType) array[0])
			{
				case PacketType.Ident: // PlayerID Packet, announces a player joining.
					return new PlayerIDPacket(array);
				case PacketType.PlayerSetBlock:
					return new BlockUpdatePacket(array);
				case PacketType.PositionUpdate:
					return new PositionUpdatePacket(array);
				case PacketType.Message:
					return new ClientMessagePacket(array);
				default:
					throw new ArgumentException("Byte array does not match any known packet");
			}
		}
	}

	/// <summary>
	/// A packet that informs the server of a player's arrival.
	/// </summary>
	public class PlayerIDPacket : ClientPacket
	{
		override public byte PacketID { get { return 0x0; } }
		public byte Version;
		public string Username;
		public string Key;
		public byte Unused;

		public PlayerIDPacket() { }

		public PlayerIDPacket(byte[] raw)
		{
			Version = raw[1];
			Username = Packet.ExtractString(raw, 2);
			Key = Packet.ExtractString(raw, 66);
			Unused = 0x00;
		}

		override public byte[] ToByteArray() {
			Builder<Byte> builder = new Builder<byte>();
			builder.Append(PacketID);
			builder.Append(Version);
			builder.Append(Packet.PackString(Username));
			builder.Append(Packet.PackString(Key));
			builder.Append(Unused);
			return builder.ToArray();
		}
	}

	/// <summary>
	/// Informing the server of a player making a change to a block.
	/// </summary>
	public class BlockUpdatePacket : ClientPacket
	{
		override public byte PacketID { get { return 0x05; } }
		public short X;
		public short Y;
		public short Z;
		public byte Mode;
		public byte Type;

		public BlockUpdatePacket() { }

		public BlockUpdatePacket(byte[] array)
		{
			X = Packet.ExtractShort(array, 1);
			Y = Packet.ExtractShort(array, 3);
			Z = Packet.ExtractShort(array, 5);
			Mode = array[7];
			Type = array[8];
		}

		override public byte[] ToByteArray()
		{
			Builder<Byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(Packet.PackShort(X));
			b.Append(Packet.PackShort(Y));
			b.Append(Packet.PackShort(Z));
			b.Append(Mode);
			b.Append(Type);
			return b.ToArray();
		}
	}

	/// <summary>
	/// Informs the server of the player's position and orientation.
	/// </summary>
	public class PositionUpdatePacket : ClientPacket
	{
		override public byte PacketID { get { return 0x08; } }
		public byte PlayerID;
		public short X;
		public short Y;
		public short Z;
		public byte Heading;
		public byte Pitch;

		public PositionUpdatePacket() { }

		public PositionUpdatePacket(byte[] array)
		{
			PlayerID = array[1];
			X = Packet.ExtractShort(array, 2);
			Y = Packet.ExtractShort(array, 4);
			Z = Packet.ExtractShort(array, 6);
			Heading = array[8];
			Pitch = array[9];
		}

		override public byte[] ToByteArray()
		{
			Builder<Byte> builder = new Builder<byte>();
			builder.Append(PacketID);
			builder.Append(PlayerID);
			builder.Append(Packet.PackShort(X));
			builder.Append(Packet.PackShort(Y));
			builder.Append(Packet.PackShort(Z));
			builder.Append(Heading);
			builder.Append(Pitch);
			return builder.ToArray();
		}
	}

	/// <summary>
	/// Message sent by the client to the server for processing.
	/// </summary>
	public class ClientMessagePacket : ClientPacket
	{
		override public byte PacketID { get { return 0x0d; } }
		public byte Unused;
		public string Message;

		public ClientMessagePacket() { }

		public ClientMessagePacket(byte[] input)
		{
			Unused = input[1];
			Message = Packet.ExtractString(input, 2);
		}

		override public byte[] ToByteArray()
		{
			Builder<Byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(Unused);
			b.Append(Packet.PackString(Message));
			return b.ToArray();
		}
	}
}
