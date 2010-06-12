using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Reflection;

namespace spacecraft
{
	public abstract partial class Packet  // Continued in Constants.cs, which defines enums.
	{
		protected const int BYTE_LENGTH = 1;
		protected const int SHORT_LENGTH = 2;
		protected const int STRING_LENGTH = NetworkString.Size;
		protected const int ARRAY_LENGTH = NetworkByteArray.Size;

		abstract public byte PacketID { get; }

		abstract public byte[] ToByteArray();
		public static implicit operator byte[](Packet P) { return P.ToByteArray(); }
	}

	/// <summary>
	/// Packets that are being sent by the client, received by the server.
	/// </summary>
	public abstract class ClientPacket : Packet
	{
		public static ClientPacket FromByteArray(byte[] array)
		{
			ClientPacket OutValue;

			byte PacketID = array[0];

			switch (PacketID)
			{
				case 0x00: // PlayerID Packet, announces a player joining.
					OutValue = new PlayerIDPacket(array);
					break;
				case 0x05:
					OutValue = new BlockUpdatePacket(array);
					break;
				case 0x08:
					OutValue = new PositionUpdatePacket(array);
					break;
				case 0x0d:
					OutValue = new ClientMessagePacket(array);
					break;
				default:
					throw new ArgumentException("Byte array does not match any known packet");
			}

			return OutValue;
		}
	}

	/// <summary>
	/// A packet that informs the server of a player's arrival.
	/// </summary>
	public class PlayerIDPacket : ClientPacket
	{
		override public byte PacketID { get { return 0x0; } }
		public byte Version;
		public NetworkString Username;
		public NetworkString Key;
		public byte Unknown; // Unused.

		public PlayerIDPacket() { }

		public PlayerIDPacket(byte[] raw)
		{
			Version = raw[1];

			byte[] name = new byte[NetworkString.Size];

			//TODO: Find a decent way of getting rid of the magic offsets.
			for (int i = 0; i < name.Length; i++)
			{
				name[i] = raw[2 + i];
			}

			Username = new NetworkString(name);

			byte[] key_bytes = new byte[NetworkString.Size];

			for (int i = 0; i < name.Length; i++)
			{
				key_bytes[i] = raw[2 + NetworkString.Size + i];
			}

			Key = new NetworkString(key_bytes);

			Unknown = 0xFF; // Fix this if this is actually used somewhere.
		}

		override public byte[] ToByteArray()
		{
			Builder<Byte> builder = new Builder<byte>();
			builder.Append(PacketID);
			builder.Append(Version);
			builder.Append(Username);
			builder.Append(Key);
			builder.Append(Unknown);
			return builder.ToArray();
		}
	}

	/// <summary>
	/// Informing the server of a player making a change to a block.
	/// </summary>
	public class BlockUpdatePacket : ClientPacket
	{
		override public byte PacketID { get { return 0x05; } }
		public NetworkShort X;
		public NetworkShort Y;
		public NetworkShort Z;
		public byte Mode;
		public byte Type;

		public BlockUpdatePacket() { }

		public BlockUpdatePacket(byte[] array)
		{
			X = new NetworkShort(array, 1);
			Y = new NetworkShort(array, 1 + NetworkShort.Size);
			Z = new NetworkShort(array, 1 + 2 * NetworkShort.Size);
			Mode = array[1 + 3 * NetworkShort.Size];
			Type = array[1 + 3 * NetworkShort.Size + 1];
		}

		override public byte[] ToByteArray()
		{
			Builder<Byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(X);
			b.Append(Y);
			b.Append(Z);
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
		public NetworkShort X;
		public NetworkShort Y;
		public NetworkShort Z;
		public byte Heading;
		public byte Pitch;

		public PositionUpdatePacket() { }

		public PositionUpdatePacket(byte[] array)
		{
			PlayerID = array[1];
			X = new NetworkShort(array, 2);
			Y = new NetworkShort(array, 2 + NetworkShort.Size);
			Z = new NetworkShort(array, 2 + 2 * NetworkShort.Size);
			Heading = array[2 + 3 * NetworkShort.Size];
			Pitch = array[2 + 3 * NetworkShort.Size + 1];
		}

		override public byte[] ToByteArray()
		{
			Builder<Byte> builder = new Builder<byte>();
			builder.Append(PacketID);
			builder.Append(PlayerID);
			builder.Append(X);
			builder.Append(Y);
			builder.Append(Z);
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
		public NetworkString Message;

		public ClientMessagePacket() { }

		public ClientMessagePacket(byte[] input)
		{
			Unused = input[1];
			byte[] namebytes = new byte[NetworkString.Size];
			Array.Copy(input, 2, namebytes, 0, NetworkString.Size);
			Message = new NetworkString(namebytes);
		}

		override public byte[] ToByteArray()
		{
			Builder<Byte> b = new Builder<byte>();
			b.Append(PacketID);
			b.Append(Unused);
			b.Append(Message);
			return b.ToArray();
		}
	}
}
