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
    }
    /// <summary>
    /// Packets that are being sent by the server.
    /// </summary>
    public abstract class ServerPacket : Packet
    {
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

    public class MessagePacket : ClientPacket
    {
        override public byte PacketID { get { return 0x0d; } }
        public byte Unused;
        public NetworkString Message;

        override public byte[] ToByteArray()
        {
            Builder<Byte> b = new Builder<byte>();
            b.Append(PacketID);
            b.Append(Unused);
            b.Append(Message);
            return b.ToArray();
        }
    }

    /* ================================================================================================================================
     * ================================================================================================================================
     * ================================================================================================================================ 
     */

    /// <summary>
    /// Confirms to the client that the player has been connected.
    /// </summary>
    public class PlayerInPacket : ServerPacket
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

}