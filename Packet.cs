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
        public byte[] Username;
        public byte[] Key;
        public byte Unknown; // Unused.

        override public byte[] ToByteArray()
        {
            byte[] buff = new byte[1 + 1 + 1024 + 1024 + 1];

            buff[0] = PacketID;
            buff[1] = Version;
            Username.CopyTo(buff, 2);
            Key.CopyTo(buff, 2 + Username.Length);
            buff[2 + Username.Length + Key.Length] = Unknown;
            return buff;
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

        override public byte[] ToByteArray()
        {
            byte[] buff = new byte[1 + 2 + 2 + 2 + 1 + 1];

            buff[0] = PacketID;
            buff[1] = (byte)(X >> 8);
            buff[2] = (byte)(X % 65536);

            buff[3] = (byte)(Y >> 8);
            buff[4] = (byte)(Y % 65536);

            buff[5] = (byte)(Z >> 8);
            buff[6] = (byte)(Z % 65536);

            buff[7] = Mode;
            buff[8] = Type;

            return buff;
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

        override public byte[] ToByteArray()
        {
            throw new NotImplementedException();
        }
    }

    public class MessagePacket : ClientPacket
    {
        override public byte PacketID { get { return 0x0d; } }
        public byte Unused;
        public byte[] Message;

        override public byte[] ToByteArray()
        {
            throw new NotImplementedException();
        }
    }

    /* ================================================================================================================================
     * ================================================================================================================================
     * ================================================================================================================================ 
     * 
     */

    /// <summary>
    /// Confirms to the client that the player has been connected.
    /// </summary>
    public class PlayerInPacket : ServerPacket
    {
        public override byte PacketID { get { return 0x00; } }
        public byte Version;
        public byte[] Name;
        public byte[] MOTD;
        public byte Type;

        public override byte[] ToByteArray()
        {
            throw new NotImplementedException();
        }
    }

}